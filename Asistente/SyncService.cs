using SQLite;
using Supabase;

namespace Asistente
{
    /// <summary>
    /// Sincroniza las tareas entre SQLite (la fuente de verdad local) y Supabase,
    /// la tabla "tarea" que comparte con Proyectito.
    ///
    /// El orden importa: primero se sube lo que el usuario hizo en esta app y
    /// después se baja lo que cambió en el otro lado. Así, un cambio hecho en
    /// Proyectito (completar, editar, borrar) aparece en Asistente, y al revés.
    ///
    /// Reglas de conflicto, ya que la tabla no lleva marca de tiempo:
    ///   - Si la fila local tiene cambios sin enviar (IsSynced = false), manda el
    ///     cambio local y la fila no se sobrescribe al bajar del servidor.
    ///   - Si la fila local ya está sincronizada, manda el servidor.
    ///   - Los borrados locales se propagan al servidor y se limpian de SQLite.
    ///   - Las filas que el servidor ya no tiene (borradas en otro dispositivo)
    ///     se borran también de SQLite.
    /// </summary>
    public class SyncService
    {
        /// <summary>
        /// Evita que dos sincronizaciones se pisen: la de segundo plano, el
        /// temporizador de la ventana y el botón de actualizar pueden pedirla a
        /// la vez. Quien pierde la carrera simplemente no hace nada esta ronda.
        /// </summary>
        private static readonly SemaphoreSlim SincronizacionEnCurso = new(1, 1);

        /// <summary>
        /// Se activa cuando el servidor rechaza "frecuencia_recordatorio_horas".
        /// Mientras dure la sesión ya no se compara ni se manda ese campo: si se
        /// siguiera comparando, el PULL vería siempre "local con valor, servidor
        /// sin columna" y borraría la frecuencia del usuario en cada ronda.
        /// </summary>
        private static bool _frecuenciaNoSoportada;

        private readonly Supabase.Client _supabase;

        public SyncService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task SincronizarTareasAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
            if (UserSession.CurrentUserId == 0) return;

            if (!await SincronizacionEnCurso.WaitAsync(0)) return;

            try
            {
                var db = await BaseDatos.ObtenerAsync();
                int idUsuario = UserSession.CurrentUserId;

                // La tabla local se crea aquí además de en BaseDatos para que el
                // servicio siga siendo utilizable aunque se construya aislado.
                await db.CreateTableAsync<TareaLocal>();

                await PropagarBorradosAsync(db, idUsuario);
                await PropagarCambiosAsync(db, idUsuario);
                await TraerDelServidorAsync(db, idUsuario);
            }
            catch (Exception ex)
            {
                await AvisarErrorAsync(ex);
            }
            finally
            {
                SincronizacionEnCurso.Release();
            }
        }

        #region PUSH

        /// <summary>
        /// Borra en Supabase las tareas marcadas como eliminadas y, si el servidor
        /// confirma, las quita también de SQLite. Si la llamada falla la fila se
        /// queda marcada para reintentar en la próxima ronda.
        /// </summary>
        private async Task PropagarBorradosAsync(SQLiteAsyncConnection db, int idUsuario)
        {
            var borradas = await db.Table<TareaLocal>()
                .Where(t => t.IdUsuario == idUsuario && t.IsDeleted)
                .ToListAsync();

            foreach (var borrada in borradas)
            {
                try
                {
                    if (borrada.IdTareaServer.HasValue)
                    {
                        await _supabase.From<Tarea>()
                            .Where(x => x.IdTarea == borrada.IdTareaServer.Value)
                            .Delete();
                    }

                    await db.DeleteAsync(borrada);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"SyncService: no se pudo borrar la tarea {borrada.IdLocal} en el servidor: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sube las tareas nuevas y las editadas. Cada fila va por separado: si una
        /// falla (por ejemplo, una columna que el servidor no tiene) las demás
        /// siguen subiendo y esa queda pendiente para el siguiente intento.
        /// </summary>
        private async Task PropagarCambiosAsync(SQLiteAsyncConnection db, int idUsuario)
        {
            var pendientes = await db.Table<TareaLocal>()
                .Where(t => t.IdUsuario == idUsuario && !t.IsSynced && !t.IsDeleted)
                .ToListAsync();

            foreach (var local in pendientes)
            {
                try
                {
                    if (local.IdTareaServer.HasValue)
                    {
                        await ActualizarEnServidorAsync(local);
                    }
                    else
                    {
                        await InsertarEnServidorAsync(local);
                    }

                    local.IsSynced = true;
                    await db.UpdateAsync(local);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"SyncService: la tarea {local.IdLocal} sigue pendiente de enviar: {ex.Message}");
                }
            }
        }

        private async Task InsertarEnServidorAsync(TareaLocal local)
        {
            int? idInsertada;

            try
            {
                var respuesta = await _supabase.From<Tarea>().Insert(new Tarea
                {
                    IdUsuario = local.IdUsuario,
                    Titulo = local.Titulo,
                    Descripcion = local.Descripcion,
                    FechaVencimiento = local.FechaVencimiento,
                    FrecuenciaRecordatorioHoras = local.FrecuenciaRecordatorioHoras,
                    Estado = EstadoTarea.Normalizar(local.Estado)
                });

                idInsertada = respuesta.Models.FirstOrDefault()?.IdTarea;
            }
            catch (Exception ex) when (EsColumnaDesconocida(ex))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"SyncService: la tabla del servidor no admite la frecuencia; la tarea {local.IdLocal} se sube sin ella ({ex.Message})");
                _frecuenciaNoSoportada = true;

                var respuesta = await _supabase.From<TareaSinFrecuencia>().Insert(new TareaSinFrecuencia
                {
                    IdUsuario = local.IdUsuario,
                    Titulo = local.Titulo,
                    Descripcion = local.Descripcion,
                    FechaVencimiento = local.FechaVencimiento,
                    Estado = EstadoTarea.Normalizar(local.Estado)
                });

                idInsertada = respuesta.Models.FirstOrDefault()?.IdTarea;
            }

            if (idInsertada is > 0)
            {
                local.IdTareaServer = idInsertada;
                return;
            }

            // Sin la fila devuelta no se puede guardar el id del servidor. Antes
            // de rendirse se reintenta el insert: si la tarea ya existía, el
            // servidor no acepta el duplicado y así se recupera su id en vez de
            // dejar la tarea local huérfana y crear un duplicado en cada ronda.
            var existentes = await _supabase.From<Tarea>()
                .Where(x => x.IdUsuario == local.IdUsuario && x.Titulo == local.Titulo)
                .Get();

            var encontrada = existentes.Models.FirstOrDefault();
            if (encontrada is not null)
            {
                local.IdTareaServer = encontrada.IdTarea;
                return;
            }

            throw new InvalidOperationException(
                "El servidor aceptó la tarea pero no devolvió su identificador.");
        }

        private async Task ActualizarEnServidorAsync(TareaLocal local)
        {
            try
            {
                await _supabase.From<Tarea>()
                    .Where(x => x.IdTarea == local.IdTareaServer!.Value)
                    .Set(x => x.Titulo, local.Titulo)
                    .Set(x => x.Descripcion, local.Descripcion)
                    .Set(x => x.FechaVencimiento, local.FechaVencimiento)
                    .Set(x => x.FrecuenciaRecordatorioHoras, local.FrecuenciaRecordatorioHoras)
                    .Set(x => x.Estado, EstadoTarea.Normalizar(local.Estado))
                    .Update();
            }
            catch (Exception ex) when (EsColumnaDesconocida(ex))
            {
                // "frecuencia_recordatorio_horas" es propia de Asistente: si la
                // tabla compartida no la tiene, se reintenta sin ella para que el
                // resto de los campos (título, fecha y estado) sí lleguen a
                // Proyectito en vez de quedarse toda la tarea sin sincronizar.
                System.Diagnostics.Debug.WriteLine(
                    $"SyncService: la tabla del servidor no admite la frecuencia; se envía sin ella ({ex.Message})");
                _frecuenciaNoSoportada = true;

                await _supabase.From<Tarea>()
                    .Where(x => x.IdTarea == local.IdTareaServer!.Value)
                    .Set(x => x.Titulo, local.Titulo)
                    .Set(x => x.Descripcion, local.Descripcion)
                    .Set(x => x.FechaVencimiento, local.FechaVencimiento)
                    .Set(x => x.Estado, EstadoTarea.Normalizar(local.Estado))
                    .Update();
            }
        }

        /// <summary>
        /// PostgREST responde con un 400 y menciona la columna cuando la petición
        /// incluye un campo que no existe en la tabla.
        /// </summary>
        private static bool EsColumnaDesconocida(Exception ex)
        {
            string mensaje = ex.ToString();
            return mensaje.Contains("column", StringComparison.OrdinalIgnoreCase) &&
                   (mensaje.Contains("frecuencia_recordatorio_horas", StringComparison.OrdinalIgnoreCase) ||
                    mensaje.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                    mensaje.Contains("not exist", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region PULL

        /// <summary>
        /// Trae el estado del servidor a SQLite. Inserta lo que aún no existe y
        /// actualiza lo que cambió en el otro dispositivo. Si el servidor no
        /// devuelve una tarea que ya estaba sincronizada, es que la borraron en
        /// otro lado: también se borra aquí.
        /// </summary>
        private async Task TraerDelServidorAsync(SQLiteAsyncConnection db, int idUsuario)
        {
            var respuesta = await _supabase.From<Tarea>()
                .Where(t => t.IdUsuario == idUsuario)
                .Get();

            var locales = await db.Table<TareaLocal>()
                .Where(t => t.IdUsuario == idUsuario && !t.IsDeleted)
                .ToListAsync();

            var porIdServidor = new Dictionary<int, TareaLocal>();
            var duplicadasEliminadas = new HashSet<int>();
            foreach (var grupo in locales
                         .Where(t => t.IdTareaServer.HasValue)
                         .GroupBy(t => t.IdTareaServer!.Value))
            {
                porIdServidor[grupo.Key] = grupo.First();

                // Si el mismo id de servidor quedó en dos filas locales (por una
                // versión anterior o por un id mal guardado), se queda una sola:
                // si no, la tarea aparecería duplicada en la lista.
                foreach (var duplicada in grupo.Skip(1))
                {
                    await db.DeleteAsync(duplicada);
                    duplicadasEliminadas.Add(duplicada.IdLocal);
                }
            }

            var idsDelServidor = new HashSet<int>();

            foreach (var delServidor in respuesta.Models)
            {
                if (delServidor.IdTarea <= 0) continue;
                idsDelServidor.Add(delServidor.IdTarea);

                if (!porIdServidor.TryGetValue(delServidor.IdTarea, out var local))
                {
                    await db.InsertAsync(new TareaLocal
                    {
                        IdTareaServer = delServidor.IdTarea,
                        IdUsuario = delServidor.IdUsuario,
                        Titulo = delServidor.Titulo ?? string.Empty,
                        Descripcion = delServidor.Descripcion,
                        FechaVencimiento = delServidor.FechaVencimiento,
                        FrecuenciaRecordatorioHoras = _frecuenciaNoSoportada
                            ? null
                            : delServidor.FrecuenciaRecordatorioHoras,
                        Estado = EstadoTarea.Normalizar(delServidor.Estado),
                        IsSynced = true,
                        UltimaModificacion = DateTime.Now
                    });
                    continue;
                }

                // Hay cambios locales sin enviar: mandan ellos, no el servidor.
                if (!local.IsSynced) continue;

                if (AplicarServidorSobreLocal(local, delServidor))
                {
                    // UltimaModificacion es la ancla de los recordatorios
                    // cíclicos, así que se refresca para que el aviso siga el
                    // ritmo del nuevo plazo, igual que al editar la tarea aquí.
                    local.UltimaModificacion = DateTime.Now;
                    await db.UpdateAsync(local);

                    ServicioFondo.ReiniciarRecordatorio(local.IdLocal);
                }
            }

            foreach (var local in locales)
            {
                if (duplicadasEliminadas.Contains(local.IdLocal)) continue;

                // Solo se propaga la baja de lo que el servidor ya conoce. Una fila
                // con cambios sin enviar puede estar ausente porque su push falló
                // (por ejemplo, sin internet a media subida): borrarla aquí tiraría
                // el trabajo del usuario, así que se conserva y se reintenta.
                if (!local.IsSynced) continue;

                if (local.IdTareaServer.HasValue && !idsDelServidor.Contains(local.IdTareaServer.Value))
                {
                    await db.DeleteAsync(local);
                    ServicioFondo.ReiniciarRecordatorio(local.IdLocal);
                }
            }
        }

        /// <summary>
        /// Copia los valores del servidor sobre la fila local. Devuelve true si
        /// algo cambió de verdad (para no reescribir la fila en cada sincronización).
        /// </summary>
        private static bool AplicarServidorSobreLocal(TareaLocal local, Tarea delServidor)
        {
            bool cambio = false;

            string titulo = delServidor.Titulo ?? string.Empty;
            if (!string.Equals(local.Titulo, titulo, StringComparison.Ordinal))
            {
                local.Titulo = titulo;
                cambio = true;
            }

            if (!string.Equals(local.Descripcion, delServidor.Descripcion, StringComparison.Ordinal))
            {
                local.Descripcion = delServidor.Descripcion;
                cambio = true;
            }

            if (!MismaFecha(local.FechaVencimiento, delServidor.FechaVencimiento))
            {
                local.FechaVencimiento = delServidor.FechaVencimiento;
                cambio = true;
            }

            if (!_frecuenciaNoSoportada
                && local.FrecuenciaRecordatorioHoras != delServidor.FrecuenciaRecordatorioHoras)
            {
                local.FrecuenciaRecordatorioHoras = delServidor.FrecuenciaRecordatorioHoras;
                cambio = true;
            }

            string estado = EstadoTarea.Normalizar(delServidor.Estado);
            if (!string.Equals(EstadoTarea.Normalizar(local.Estado), estado, StringComparison.Ordinal))
            {
                local.Estado = estado;
                cambio = true;
            }

            return cambio;
        }

        /// <summary>
        /// Compara fechas sin que el viaje por Supabase (que devuelve UTC) haga
        /// parecer que algo cambió cuando no. Se tolera un minuto de diferencia
        /// porque el servidor puede recortar los segundos.
        /// </summary>
        private static bool MismaFecha(DateTime? local, DateTime? delServidor)
        {
            if (!local.HasValue || !delServidor.HasValue) return local.HasValue == delServidor.HasValue;

            var a = AFechaLocal(local.Value);
            var b = AFechaLocal(delServidor.Value);

            return Math.Abs((a - b).TotalMinutes) < 1;
        }

        private static DateTime AFechaLocal(DateTime fecha)
            => fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha;

        #endregion

        private static async Task AvisarErrorAsync(Exception ex)
        {
            // El aviso solo se muestra si el usuario está mirando la app.
            // Si la app está oculta en la bandeja, saltar un aviso a pantalla
            // completa sería molesto (y puede quedar detrás de cualquier otra
            // ventana), así que en ese caso solo se deja en el log.
            System.Diagnostics.Debug.WriteLine($"Error al sincronizar con el servidor: {ex.Message}");

            if (!ServicioFondo.EnPrimerPlano) return;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (page != null)
                    {
                        await page.DisplayAlertAsync("Error de sincronización",
                            "No se pudo sincronizar las tareas con el servidor.\n\nDetalle: " + ex.Message,
                            "OK");
                    }
                });
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo mostrar el aviso de error: {ex2.Message}");
            }
        }
    }
}
