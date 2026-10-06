using SQLite;
using Supabase;

namespace Asistente
{
    /// <summary>
    /// Servicio en segundo plano con la vida de la APLICACIÓN (no de la página).
    ///
    /// Antes el temporizador de sincronización y el aviso de recordatorios vivían
    /// en MainPage, así que al cerrar la ventana se acababan los dos. Este servicio
    /// se arranca desde App.CreateWindow y sigue funcionando con la ventana oculta
    /// en la bandeja del sistema, de modo que los recordatorios siguen saliendo.
    ///
    /// Hace tres cosas, todas sin depender de internet:
    ///   1. Evaluar los recordatorios de las tareas y mostrar los avisos que toquen.
    ///   2. Dejar una copia del próximo aviso programado en el sistema operativo,
    ///      para que también salte si el proceso muere.
    ///   3. Sincronizar con Supabase en cuanto hay red (y avisar a la interfaz).
    /// </summary>
    public static class ServicioFondo
    {
        private const int IntervaloEvaluacionSegundos = 30;
        private const int IntervaloSincronizacionSegundos = 60;

        /// <summary>
        /// Margen de cortesía para dar por bueno un aviso que entregó el sistema
        /// operativo. Si el toast ya no está en su cola pero aún no ha pasado
        /// mucho tiempo desde su hora, se considera mostrado y no se repite.
        /// Pasado ese margen se vuelve a avisar, por si el sistema lo perdió.
        /// </summary>
        private static readonly TimeSpan VentanaCortesia = TimeSpan.FromMinutes(3);

        private static Supabase.Client? _supabase;
        private static SyncService? _syncService;

        private static IDispatcherTimer? _temporizadorRecordatorios;
        private static IDispatcherTimer? _temporizadorSync;
        private static readonly object Cerradura = new();

    private static bool _evaluando;
    private static bool _sincronizando;

    // Renovar el token de Supabase solo puede ocurrir una vez a la vez: las
    // llamadas que pierdan la carrera se saltan esta ronda en lugar de esperar.
    private static readonly SemaphoreSlim _renovandoSesion = new(1, 1);
        private static bool _arrancado;

        /// <summary>
        /// True mientras el usuario tiene la ventana a la vista. Se usa para no
        /// lanzar avisos a pantalla cuando la app está oculta en la bandeja.
        /// </summary>
        public static bool EnPrimerPlano { get; set; } = true;

        /// <summary>
        /// Se dispara (en el hilo principal) cuando la sincronización en segundo
        /// plano ha traido cambios, para que la interfaz se refresque sola.
        /// </summary>
        public static event Action? DatosSincronizados;

        #region Servicios compartidos

        /// <summary>
        /// Cliente de Supabase único para toda la app. Antes cada pantalla creaba el
        /// suyo, así que quedaban varios clientes con sesiones distintas.
        /// </summary>
        public static Supabase.Client ClienteSupabase
        {
            get
            {
                if (_supabase != null) return _supabase;

                lock (Cerradura)
                {
                    _supabase ??= new Supabase.Client(
                        Configuracion.SupabaseUrl,
                        Configuracion.SupabaseAnonKey,
                        new SupabaseOptions
                        {
                            AutoRefreshToken = true,
                            AutoConnectRealtime = true
                        });

                    return _supabase;
                }
            }
        }

        public static SyncService ServicioSync
        {
            get
            {
                if (_syncService != null) return _syncService;

                lock (Cerradura)
                {
                    _syncService ??= new SyncService(ClienteSupabase);
                    return _syncService;
                }
            }
        }

        #endregion

        #region Arranque y parada

        /// <summary>
        /// Arranca el servicio. Es idempotente: llamarlo varias veces no duplica
        /// los temporizadores.
        /// </summary>
        public static void Arrancar()
        {
            if (_arrancado) return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null) return;

            _arrancado = true;

            _temporizadorRecordatorios = dispatcher.CreateTimer();
            _temporizadorRecordatorios.Interval = TimeSpan.FromSeconds(IntervaloEvaluacionSegundos);
            _temporizadorRecordatorios.Tick += (_, _) => _ = EvaluarRecordatoriosAsync();
            _temporizadorRecordatorios.Start();

            _temporizadorSync = dispatcher.CreateTimer();
            _temporizadorSync.Interval = TimeSpan.FromSeconds(IntervaloSincronizacionSegundos);
            _temporizadorSync.Tick += (_, _) => _ = SincronizarEnSegundoPlanoAsync();
            _temporizadorSync.Start();

            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

            // Primera pasada inmediata para no tener que esperar al primer tick.
            _ = EvaluarRecordatoriosAsync();
            _ = SincronizarEnSegundoPlanoAsync();
        }

        public static void Detener()
        {
            _temporizadorRecordatorios?.Stop();
            _temporizadorRecordatorios = null;

            _temporizadorSync?.Stop();
            _temporizadorSync = null;

            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
            _arrancado = false;
        }

        private static async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != NetworkAccess.Internet) return;

            // Al volver la red hay que renovar el token antes de sincronizar,
            // y de paso reevaluar: puede haber tareas nuevas de otro dispositivo.
            await AsegurarSesionSupabaseAsync();
            await SincronizarEnSegundoPlanoAsync();
            await EvaluarRecordatoriosAsync();
        }

        #endregion

        #region Sincronización

        /// <summary>
        /// Sincroniza con Supabase si hay red y avisa a la interfaz. No hace nada
        /// (y no es un error) cuando no hay conexión: la app sigue con SQLite.
        /// </summary>
        public static async Task SincronizarEnSegundoPlanoAsync()
        {
            if (_sincronizando) return;
            if (UserSession.CurrentUserId == 0) return;
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            _sincronizando = true;
            try
            {
                await AsegurarSesionSupabaseAsync();
                await ServicioSync.SincronizarTareasAsync();

                // El rol puede haber cambiado en el servidor (por ejemplo, si otro
                // administrador le concedió o le quitó el de admin). Consultarlo
                // aquí hace que el panel aparezca sin obligar a reiniciar sesión.
                await AdminService.RefrescarRolAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try { DatosSincronizados?.Invoke(); } catch { }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServicioFondo: error al sincronizar: {ex.Message}");
            }
            finally
            {
                _sincronizando = false;
            }
        }

        /// <summary>
        /// Deja un JWT válido en Supabase sin obligar al usuario a volver a
        /// iniciar sesión:
        ///   A) Entró sin conexión (no hay JWT): inicia sesión con las credenciales
        ///      guardadas para activar el token.
        ///   B) Hay JWT guardado pero caducado: lo renueva con el refresh token.
        /// </summary>
        public static async Task AsegurarSesionSupabaseAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            // Se la llama desde el arranque de la ventana, el temporizador, el
            // retorno de la conexión y el propio panel. Renovar el token a la vez
            // desde varios frentes dejaba la interfaz esperando, así que si ya hay
            // una renovación en marcha esta se salta: la otra está haciéndola.
            if (!await _renovandoSesion.WaitAsync(0)) return;

            try
            {
                var cliente = ClienteSupabase;
                await cliente.InitializeAsync();

                // A) Sesión iniciada offline -> iniciar sesión real para activar el token.
                if (string.IsNullOrEmpty(UserSession.CurrentJwt) &&
                    !string.IsNullOrEmpty(UserSession.OfflineEmail) &&
                    !string.IsNullOrEmpty(UserSession.OfflinePassword))
                {
                    var session = await cliente.Auth.SignIn(UserSession.OfflineEmail, UserSession.OfflinePassword);
                    if (session?.User != null)
                    {
                        UserSession.CurrentJwt = session.AccessToken ?? "";
                        UserSession.CurrentRefreshToken = session.RefreshToken ?? "";
                        UserSession.CurrentAuthId = session.User.Id;
                        UserSession.OfflineEmail = string.Empty;
                        UserSession.OfflinePassword = string.Empty;
                        UserSession.GuardarSesion();
                    }
                    return;
                }

                // B) Hay JWT guardado pero el cliente no tiene sesión activa: renovar.
                //    El access token expira (~1h); el refresh token lo renueva sin relogin.
                if (cliente.Auth.CurrentSession == null &&
                    !string.IsNullOrEmpty(UserSession.CurrentJwt) &&
                    !string.IsNullOrEmpty(UserSession.CurrentRefreshToken))
                {
                    var refrescada = await cliente.Auth.SetSession(
                        UserSession.CurrentJwt, UserSession.CurrentRefreshToken, forceAccessTokenRefresh: true);

                    if (refrescada != null && !string.IsNullOrEmpty(refrescada.AccessToken))
                    {
                        UserSession.CurrentJwt = refrescada.AccessToken;
                        UserSession.CurrentRefreshToken = refrescada.RefreshToken ?? "";
                        UserSession.CurrentAuthId = refrescada.User?.Id ?? UserSession.CurrentAuthId;
                        UserSession.GuardarSesion();
                    }
                }
            }
            catch (Exception ex)
            {
                Depurador.Registrar("ServicioFondo.AsegurarSesionSupabaseAsync", ex);
                System.Diagnostics.Debug.WriteLine($"ServicioFondo: error al renovar la sesión: {ex.Message}");
            }
            finally
            {
                _renovandoSesion.Release();
            }
        }

        #endregion

        #region Recordatorios

        /// <summary>
        /// Recorre las tareas pendientes de la BD local y muestra los recordatorios
        /// que toquen. Lee siempre de SQLite, así que funciona sin conexión.
        /// </summary>
        public static async Task EvaluarRecordatoriosAsync()
        {
            if (_evaluando) return;

            _evaluando = true;
            try
            {
                if (UserSession.CurrentUserId == 0) return;

                var db = await BaseDatos.ObtenerAsync();

                var tareas = (await db.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == UserSession.CurrentUserId
                             && !t.IsDeleted)
                    .ToListAsync())
                    .Where(t => !EstadoTarea.EsCompletado(t.Estado))
                    .ToList();

                foreach (var tarea in tareas)
                {
                    await EvaluarTareaAsync(db, tarea);
                }

                await LimpiarEstadosHuerfanosAsync(db, tareas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServicioFondo: error al evaluar recordatorios: {ex.Message}");
            }
            finally
            {
                _evaluando = false;
            }
        }

        private static async Task EvaluarTareaAsync(SQLiteAsyncConnection db, TareaLocal tarea)
        {
            var estado = await db.Table<RecordatorioEstado>()
                .FirstOrDefaultAsync(e => e.IdTarea == tarea.IdLocal);

            // La tarea se quedó sin recordatorio: fuera estados y toasts que sobren.
            if ((tarea.FrecuenciaRecordatorioHoras ?? 0) <= 0 && !tarea.FechaVencimiento.HasValue)
            {
                if (estado != null) await db.DeleteAsync(estado);
                NotificadorTareas.CancelarRecordatorio(tarea.IdLocal);
                return;
            }

            // En móvil y Mac el sistema ya se encarga de repetir el aviso aunque la
            // app esté cerrada, así que aquí solo se lleva el estado al día.
            if (NotificadorTareas.ElSistemaRepiteElAviso)
            {
                await SincronizarEstadoSinMostrarAsync(db, tarea, estado);
                return;
            }

            // Primera vez que se ve esta tarea: fijar la próxima ocurrencia sin
            // disparar nada todavía (evita una avalancha de avisos al abrir la app).
            if (estado == null)
            {
                var primera = CalcularProximaOcurrencia(tarea, DateTime.MinValue, repeticion: true);
                if (primera == null) return;

                estado = new RecordatorioEstado
                {
                    IdTarea = tarea.IdLocal,
                    ProximaOcurrencia = primera.Value,
                    ProgramadoEnSistema = false
                };
                await db.InsertAsync(estado);
            }

            var ahora = DateTime.Now;
            if (ahora < estado.ProximaOcurrencia)
            {
                // Todavía no toca. Se deja una copia agendada en el sistema
                // operativo por si el proceso muere antes de la hora.
                await AsegurarCopiaEnSistemaAsync(db, tarea, estado);
                return;
            }

            // Toca avisar. Si el sistema operativo ya mostró este mismo aviso
            // (porque el proceso estaba muerto), no se repite.
            if (!ElSistemaYaMostroElAviso(tarea, estado))
            {
                NotificadorTareas.MostrarRecordatorio(tarea);
            }

            estado.UltimoAviso = ahora;
            estado.ProgramadoEnSistema = false;

            // El aviso ya salió: se retira la copia del sistema para que no salte otra vez.
            NotificadorTareas.CancelarRecordatorio(tarea.IdLocal);

            var siguiente = CalcularProximaOcurrencia(tarea, ahora, repeticion: true);
            if (siguiente == null)
            {
                // Recordatorio puntual ya cumplido: no queda nada que avisar.
                await db.DeleteAsync(estado);
            }
            else
            {
                estado.ProximaOcurrencia = siguiente.Value;
                await db.UpdateAsync(estado);
            }
        }

        /// <summary>
        /// En plataformas donde el sistema repite el aviso por su cuenta (Android,
        /// iOS, MacCatalyst) solo se actualiza el estado, sin mostrar nada: el
        /// aviso ya lo pone el SO.
        ///
        /// Al crear el estado por primera vez se agenda la notificación en el
        /// sistema, que es lo que garantiza que siga llegando con la app cerrada
        /// o el móvil bloqueado. Solo se agenda una vez por tarea: si se
        /// re-agendara en cada comprobación, la hora del aviso se iría desplazando
        /// y acabaría saltando siempre "dentro de 5 segundos".
        /// </summary>
        private static async Task SincronizarEstadoSinMostrarAsync(
            SQLiteAsyncConnection db, TareaLocal tarea, RecordatorioEstado? estado)
        {
            if (estado != null)
            {
                var proxima = CalcularProximaOcurrencia(tarea, estado.UltimoAviso, repeticion: true);
                if (proxima == null)
                {
                    NotificadorTareas.CancelarRecordatorio(tarea.IdLocal);
                    await db.DeleteAsync(estado);
                }
                else if (proxima.Value != estado.ProximaOcurrencia)
                {
                    estado.ProximaOcurrencia = proxima.Value;
                    await db.UpdateAsync(estado);
                }
                return;
            }

            var primera = CalcularProximaOcurrencia(tarea, DateTime.MinValue, repeticion: true);
            if (primera == null) return;

            await db.InsertAsync(new RecordatorioEstado
            {
                IdTarea = tarea.IdLocal,
                ProximaOcurrencia = primera.Value,
                ProgramadoEnSistema = false
            });

            NotificadorTareas.ProgramarRecordatorio(
                tarea.IdLocal, tarea.Titulo, tarea.FechaVencimiento, tarea.FrecuenciaRecordatorioHoras ?? 0);
        }

        /// <summary>
        /// Calcula el instante del próximo aviso de una tarea.
        ///
        /// Con frecuencia (recordatorio cíclico) devuelve la primera ocurrencia de
        /// la serie "ancla + N x frecuencia" que aún no se ha avisado. Sin
        /// frecuencia devuelve el aviso único del día anterior al vencimiento, y
        /// solo si ese momento todavía está por llegar.
        ///
        /// Devuelve null si a la tarea ya no le queda ningún aviso pendiente.
        /// </summary>
        private static DateTime? CalcularProximaOcurrencia(TareaLocal tarea, DateTime ultimaMostrada, bool repeticion)
        {
            var frecuencia = tarea.FrecuenciaRecordatorioHoras ?? 0;

            if (frecuencia > 0)
            {
                if (!repeticion) return null;

                // Ancla: cuándo se creó o editó la tarea. Si el reloj del equipo
                // se adelantó, se usa "ahora" para no programar en el pasado.
                var ancla = tarea.UltimaModificacion;
                if (ancla > DateTime.Now) ancla = DateTime.Now;

                var proxima = ancla.AddHours(frecuencia);
                while (proxima <= ultimaMostrada)
                {
                    proxima = proxima.AddHours(frecuencia);
                }

                return proxima;
            }

            if (tarea.FechaVencimiento.HasValue)
            {
                // Aviso único: un día antes del vencimiento.
                var aviso = tarea.FechaVencimiento.Value.AddDays(-1);

                // Si la fecha ya pasó no se avisa: la tarea está vencida o
                // venciéndose y el usuario ya lo tiene presente.
                if (aviso <= DateTime.Now) return null;
                if (aviso <= ultimaMostrada) return null;

                return aviso;
            }

            return null;
        }

        /// <summary>
        /// Mantiene agendada en el sistema operativo la copia del próximo aviso,
        /// para que salte aunque el proceso esté cerrado.
        ///
        /// El estado "hay copia en cola" se guarda en disco porque es lo que
        /// permite, al reiniciar, distinguir dos casos que desde dentro se ven
        /// igual: el sistema ya mostró el aviso, o simplemente lo perdimos. Sin
        /// ese dato el proceso nuevo no podría saber si tiene que avisar o no.
        ///
        /// También sirve para recuperar la copia si el sistema la borra de la cola
        /// sin llegar a mostrar (pasa al reiniciar el equipo): si ya no está, se
        /// vuelve a agendar.
        /// </summary>
        private static async Task AsegurarCopiaEnSistemaAsync(
            SQLiteAsyncConnection db, TareaLocal tarea, RecordatorioEstado estado)
        {
            try
            {
                if (NotificadorTareas.CopiaEnSistemaExiste(tarea.IdLocal))
                    return; // ya está agendada y en pie

                bool agendada = NotificadorTareas.ProgramarCopiaEnSistema(
                    tarea.IdLocal, tarea.Titulo, new DateTimeOffset(estado.ProximaOcurrencia));

                if (agendada == estado.ProgramadoEnSistema)
                    return; // nada cambió, no hace falta escribir

                estado.ProgramadoEnSistema = agendada;
                await db.UpdateAsync(estado);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServicioFondo: no se pudo agendar en el SO: {ex.Message}");
            }
        }

        /// <summary>
        /// Decide si el sistema operativo ya mostró el aviso de esta ocurrencia.
        ///
        /// La pista es que el toast desaparecer de la cola del sistema: lo que se
        /// agenda desaparece porque se mostró, no porque se haya cancelado (eso lo
        /// hace esta misma clase). Solo se acepta como válido si el aviso era
        /// reciente; si el sistema lo hubiera perdido de la cola hace tiempo, se
        /// vuelve a avisar para que el usuario no se quede sin el recordatorio.
        /// </summary>
        private static bool ElSistemaYaMostroElAviso(TareaLocal tarea, RecordatorioEstado estado)
        {
            if (!estado.ProgramadoEnSistema) return false;

#if WINDOWS
            if (!OperatingSystem.IsWindows()) return false;

            // El toast sigue en cola: todavía no se ha mostrado, lo mostramos nosotros.
            if (NotificadorWindows.ObtenerProgramada(tarea.IdLocal).HasValue) return false;

            return (DateTime.Now - estado.ProximaOcurrencia) <= VentanaCortesia;
#else
            _ = tarea;
            _ = estado;
            return false;
#endif
        }

        /// <summary>
        /// Borra el estado de recordatorio de tareas que ya no están (borradas,
        /// completadas o de otro usuario), junto con sus avisos en el sistema.
        /// </summary>
        private static async Task LimpiarEstadosHuerfanosAsync(SQLiteAsyncConnection db, List<TareaLocal> tareasVivas)
        {
            var idsVivos = tareasVivas.Select(t => t.IdLocal).ToHashSet();

            var estados = await db.Table<RecordatorioEstado>().ToListAsync();
            foreach (var estado in estados)
            {
                if (idsVivos.Contains(estado.IdTarea)) continue;

                NotificadorTareas.CancelarRecordatorio(estado.IdTarea);
                await db.DeleteAsync(estado);
            }
        }

        /// <summary>
        /// Olvida el estado de recordatorio de una tarea para que se vuelva a
        /// calcular desde cero. Se llama al crear, editar, completar o borrar una
        /// tarea: si no, el estado guardado seguiría apuntando a la frecuencia
        /// anterior y el nuevo intervalo no se aplicaría hasta el reinicio.
        /// </summary>
        public static void ReiniciarRecordatorio(int idTarea)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    NotificadorTareas.CancelarRecordatorio(idTarea);

                    var db = await BaseDatos.ObtenerAsync();
                    var estado = await db.Table<RecordatorioEstado>()
                        .FirstOrDefaultAsync(e => e.IdTarea == idTarea);

                    if (estado != null)
                        await db.DeleteAsync(estado);

                    await EvaluarRecordatoriosAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ServicioFondo: error al reiniciar recordatorio: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Cancela todos los avisos y olvida el estado de recordatorios. Se usa al
        /// cerrar sesión, para que el usuario no siga recibiendo avisos de unas
        /// tareas a las que ya no tiene acceso.
        /// </summary>
        public static async Task DetenerTodoAsync()
        {
            try
            {
                var db = await BaseDatos.ObtenerAsync();

                var estados = await db.Table<RecordatorioEstado>().ToListAsync();
                foreach (var estado in estados)
                {
                    NotificadorTareas.CancelarRecordatorio(estado.IdTarea);
                    await db.DeleteAsync(estado);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServicioFondo: error al cancelar los recordatorios: {ex.Message}");
            }
        }

        /// <summary>
        /// Deja agendados en el sistema operativo todos los recordatorios
        /// pendientes. Se invoca justo antes de cerrar el proceso (opción "Salir"
        /// de la bandeja) para que el usuario siga recibiendo avisos aunque la
        /// aplicación esté cerrada del todo.
        ///
        /// Windows permite como máximo 64 notificaciones en cola, así que se
        /// priorizan los avisos más próximos.
        /// </summary>
        public static async Task ArmarRecordatoriosParaProcesoMuerto()
        {
            try
            {
                if (UserSession.CurrentUserId == 0) return;

                var db = await BaseDatos.ObtenerAsync();

                var tareas = (await db.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == UserSession.CurrentUserId
                             && !t.IsDeleted)
                    .ToListAsync())
                    .Where(t => !EstadoTarea.EsCompletado(t.Estado))
                    .ToList();

                var proximos = new List<(DateTime Cuando, int IdTarea, string Titulo)>();

                foreach (var tarea in tareas)
                {
                    var estado = await db.Table<RecordatorioEstado>()
                        .FirstOrDefaultAsync(e => e.IdTarea == tarea.IdLocal);

                    // Sin estado todavía se descarta esta tarea: se perdería su
                    // primer aviso, pero es preferible a disparar todos de golpe
                    // la primera vez que se abre la app.
                    if (estado is null) continue;

                    if (DateTime.Now >= estado.ProximaOcurrencia) continue;

                    proximos.Add((estado.ProximaOcurrencia, tarea.IdLocal, tarea.Titulo));
                }

                foreach (var (cuando, idTarea, titulo) in proximos.OrderBy(p => p.Cuando).Take(64))
                {
                    NotificadorTareas.ProgramarCopiaEnSistema(idTarea, titulo, new DateTimeOffset(cuando));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServicioFondo: no se pudieron armar los recordatorios: {ex.Message}");
            }
        }

        #endregion
    }
}
