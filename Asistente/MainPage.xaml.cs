/*using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Asistente
{
    public partial class MainPage : ContentPage
    {
        private bool _isAnimating = false;
        private Supabase.Client _supabase;

        public MainPage()
        {
            InitializeComponent();

            string url = "https://mmvzkwklwibugzpyawmy.supabase.co";
            string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1tdnprd2tsd2lidWd6cHlhd215Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODgxODE0NTgsImV4cCI6MjEwMzc1NzQ1OH0.41uyn16H27UbRaV1aKBGGPonFw_48Q1rdsHV2TKcQp8";

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _supabase = new Supabase.Client(url, key, options);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _isAnimating = true;
            StartHudAnimations();

            await InitializeSupabaseAndLoadTasksAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isAnimating = false;
        }

        #region 1. ANIMACIONES DE LA INTERFAZ HUD

        private async void StartHudAnimations()
        {
            _ = RotateElementLoop(OuterRing1, 12000, true);
            _ = RotateElementLoop(OuterRing2, 8000, false);
            _ = RotateElementLoop(MidRing, 15000, true);
            _ = RotateElementLoop(InnerRing, 4000, false);

            while (_isAnimating)
            {
                await Task.WhenAll(
                    CoreCenter.ScaleTo(1.15, 1200, Easing.SinInOut),
                    GlowEffect.ScaleTo(1.3, 1200, Easing.SinInOut),
                    GlowEffect.FadeTo(0.3, 1200, Easing.SinInOut)
                );

                if (!_isAnimating) break;

                await Task.WhenAll(
                    CoreCenter.ScaleTo(1.0, 1200, Easing.SinInOut),
                    GlowEffect.ScaleTo(1.0, 1200, Easing.SinInOut),
                    GlowEffect.FadeTo(0.15, 1200, Easing.SinInOut)
                );
            }
        }

        private async Task RotateElementLoop(VisualElement element, uint duration, bool clockwise)
        {
            while (_isAnimating)
            {
                element.Rotation = 0;
                double targetRotation = clockwise ? 360 : -360;
                await element.RotateTo(targetRotation, duration, Easing.Linear);
            }
        }

        private async Task TriggerCorePulseAsync()
        {
            await Task.WhenAll(
                CoreCenter.ScaleTo(1.4, 150, Easing.CubicOut),
                GlowEffect.ScaleTo(1.8, 150, Easing.CubicOut)
            );
            await Task.WhenAll(
                CoreCenter.ScaleTo(1.0, 200, Easing.CubicIn),
                GlowEffect.ScaleTo(1.0, 200, Easing.CubicIn)
            );
        }

        #endregion

        #region 2. SERVIDOR Y OPERACIONES DE BASE DE DATOS

        private async Task InitializeSupabaseAndLoadTasksAsync()
        {
            try
            {
                await _supabase.InitializeAsync();
                await LoadTasksAsync();
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Modo fuera de línea. No se pudo conectar con la base de datos.";
            }
        }

        private async Task LoadTasksAsync()
        {
            // Validar que exista un usuario logueado en la sesión
            int currentUserId = UserSession.CurrentUserId;
            if (currentUserId == 0)
            {
                AiMessageLabel.Text = "Sesión no detectada. Inicia sesión nuevamente.";
                return;
            }

            try
            {
                // Cargar únicamente las tareas del usuario activo
                var response = await _supabase.From<Tarea>()
                    .Where(t => t.IdUsuario == currentUserId)
                    .Get();

                var tasks = response.Models;

                TasksCollectionView.ItemsSource = tasks;
                int pendingCount = tasks.Count(t => t.Estado != "Completado");

                await TriggerCorePulseAsync();

                string userName = string.IsNullOrEmpty(UserSession.CurrentUserName) ? "Operador" : UserSession.CurrentUserName;
                AiMessageLabel.Text = $"Bienvenido {userName}. Estado: {pendingCount} tareas pendientes.";
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Error al consultar la tabla 'tarea' en Supabase.";
            }
        }

        private async void OnAddTaskClicked(object sender, EventArgs e)
        {
            int currentUserId = UserSession.CurrentUserId;

            if (currentUserId == 0)
            {
                await DisplayAlert("Error", "No hay una sesión de usuario activa. Inicia sesión.", "OK");
                return;
            }

            string titulo = await DisplayPromptAsync("Nueva Tarea", "¿Qué deseas registrar?");
            if (string.IsNullOrWhiteSpace(titulo)) return;

            var nuevaTarea = new Tarea
            {
                IdUsuario = currentUserId, // <-- ASIGNA EL ID REAL DE LA SESIÓN
                Titulo = titulo,
                Descripcion = "Registrada desde la app mobile",
                FechaVencimiento = DateTime.Now.AddDays(1),
                Estado = "Pendiente"
            };

            try
            {
                await _supabase.From<Tarea>().Insert(nuevaTarea);
                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo crear la tarea: {ex.Message}", "OK");
            }
        }

        private async void OnGetSummaryClicked(object sender, EventArgs e)
        {
            await TriggerCorePulseAsync();

            int currentUserId = UserSession.CurrentUserId;

            try
            {
                var response = await _supabase.From<Tarea>()
                    .Where(t => t.IdUsuario == currentUserId)
                    .Get();

                var urgente = response.Models
                    .Where(t => t.Estado != "Completado" && t.FechaVencimiento.HasValue)
                    .OrderBy(t => t.FechaVencimiento)
                    .FirstOrDefault();

                if (urgente != null)
                {
                    AiMessageLabel.Text = $"Prioridad crítica: '{urgente.Titulo}' (Vence: {urgente.FechaVencimiento:dd/MM/yyyy}).";
                }
                else
                {
                    AiMessageLabel.Text = "Sin tareas pendientes. Todos los módulos operativos al 100%.";
                }
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Error al calcular el resumen de tareas.";
            }
        }

        #endregion
    }

    // CLASE MODELO MAPEADA A TU TABLA EXACTA EN SUPABASE
    [Table("tarea")]
    public class Tarea : BaseModel
    {
        [PrimaryKey("id_tarea", false)]
        public int IdTarea { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("estado")]
        public string? Estado { get; set; } = "Pendiente";
    }
}*/


using SQLite;
using Supabase;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;


namespace Asistente
{
    public partial class MainPage : ContentPage
    {
        private bool _isAnimating = false;
        private Supabase.Client _supabase;
        
        private SQLiteAsyncConnection _dbLocal;
        private SyncService _syncService;
        private AgenteIAServicio _agenteIA;
        private AgenteViewModel _agenteVM;

        private IDispatcherTimer _syncTimer;
        private bool _isSyncing = false;

        // Consola del asistente IA
        private IDispatcherTimer? _agenteRelojEtapas;
        private IDispatcherTimer? _agenteRelojPuntos;
        private bool _agenteAnimando = false;
        private int _agenteEtapa = 0;
        private int _agentePasoPuntos = 0;
        private int _agenteGeneracion = 0;
        private int _agenteSesion = 0;
        private bool _agenteOcupado = false;

        // Popup: tarea que se está editando (null = nueva)
        private TareaLocal _tareaEditando;

        // Filtro actual de visualización
        private string _filtroActual = "Pendientes";

        // Opciones de frecuencia en horas
        private readonly List<KeyValuePair<int, string>> _opcionesFrecuencia = new()
        {
            new KeyValuePair<int, string>(0, "Sin recordatorio"),
            new KeyValuePair<int, string>(1, "Cada 1 hora"),
            new KeyValuePair<int, string>(2, "Cada 2 horas"),
            new KeyValuePair<int, string>(3, "Cada 3 horas"),
            new KeyValuePair<int, string>(4, "Cada 4 horas"),
            new KeyValuePair<int, string>(6, "Cada 6 horas"),
            new KeyValuePair<int, string>(8, "Cada 8 horas"),
            new KeyValuePair<int, string>(10, "Cada 10 horas"),
            new KeyValuePair<int, string>(12, "Cada 12 horas"),
            new KeyValuePair<int, string>(24, "Cada 1 día (24 horas)"),
            new KeyValuePair<int, string>(48, "Cada 2 días (48 horas)"),
            new KeyValuePair<int, string>(72, "Cada 3 días (72 horas)"),
            new KeyValuePair<int, string>(168, "Cada 7 días (una vez por semana)")
        };

        public MainPage()
        {
            InitializeComponent();

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _supabase = new Supabase.Client(Configuracion.SupabaseUrl, Configuracion.SupabaseAnonKey, options);

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "asistente.db3");
            _dbLocal = new SQLiteAsyncConnection(dbPath);
            
            _syncService = new SyncService(dbPath, _supabase);
            _agenteIA = new AgenteIAServicio(_supabase);

            // Estado de la consola del asistente (enlaza las tarjetas del overlay)
            _agenteVM = new AgenteViewModel();
            AgentePopupOverlay.BindingContext = _agenteVM;

            // Inicializar opciones del picker de frecuencia del popup
            foreach (var opcion in _opcionesFrecuencia)
            {
                PopupFrecuenciaPicker.Items.Add(opcion.Value);
            }

            DateTime hoy = DateTime.Today;
            PopupFechaPicker.MinimumDate = hoy;
            PopupFechaPicker.MaximumDate = hoy.AddYears(5);

            // Inicializar estilo de botones de filtro
            ActualizarEstiloBotonesFiltro();
        }

        private void OnTestNotificationClicked(object sender, EventArgs e)
{
    EnviarNotificacionPC("🧪 Prueba Exitosa",
        "¡Las notificaciones están funcionando correctamente en tu dispositivo!");

    // Aviso visual para el usuario
    DisplayAlert("Prueba en marcha", "La notificación se disparará en unos segundos. Puedes minimizar la app.", "OK");
}

       protected override async void OnAppearing()
{
    base.OnAppearing();

    // Solicitar permiso de notificaciones en el teléfono si aún no se ha otorgado
    if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
    {
        await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    _isAnimating = true;
    StartHudAnimations();

    await _dbLocal.CreateTableAsync<TareaLocal>();
    await InitializeAndSyncAsync();

    // Sincronización automática cada 30 segundos mientras la app está visible
    _syncTimer = Dispatcher.CreateTimer();
    _syncTimer.Interval = TimeSpan.FromSeconds(30);
    _syncTimer.Tick += OnAutoSyncTick;
    _syncTimer.Start();
}

private async void OnAutoSyncTick(object sender, EventArgs e)
{
    // Evitar sincronizaciones simultáneas
    if (_isSyncing) return;
    _isSyncing = true;
    try
    {
        await RefreshAllAsync();
    }
    finally
    {
        _isSyncing = false;
    }
}
        
private void EnviarNotificacionPC(string titulo, string mensaje)
{
    // Muestra un toast inmediato: en Windows usa el notificador nativo del SO
    // (funciona sin internet) y en móvil el centro de notificaciones local.
    NotificadorTareas.MostrarNotificacionInmediata(titulo, mensaje);
}
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isAnimating = false;

            // Detener la sincronización automática al salir de la página
            if (_syncTimer != null)
            {
                _syncTimer.Stop();
                _syncTimer.Tick -= OnAutoSyncTick;
                _syncTimer = null;
            }

            // Detener los temporizadores y animaciones de la consola del asistente
            DetenerAnimacionesAsistente();
        }

        #region 1. ANIMACIONES DE LA INTERFAZ HUD (Sin cambios)

        private async void StartHudAnimations()
        {
            _ = RotateElementLoop(OuterRing1, 12000, true);
            _ = RotateElementLoop(OuterRing2, 8000, false);
            _ = RotateElementLoop(MidRing, 15000, true);
            _ = RotateElementLoop(InnerRing, 4000, false);

            while (_isAnimating)
            {
                await Task.WhenAll(
                    CoreCenter.ScaleTo(1.15, 1200, Easing.SinInOut),
                    GlowEffect.ScaleTo(1.3, 1200, Easing.SinInOut),
                    GlowEffect.FadeTo(0.3, 1200, Easing.SinInOut)
                );

                if (!_isAnimating) break;

                await Task.WhenAll(
                    CoreCenter.ScaleTo(1.0, 1200, Easing.SinInOut),
                    GlowEffect.ScaleTo(1.0, 1200, Easing.SinInOut),
                    GlowEffect.FadeTo(0.15, 1200, Easing.SinInOut)
                );
            }
        }

        private async Task RotateElementLoop(VisualElement element, uint duration, bool clockwise)
        {
            while (_isAnimating)
            {
                element.Rotation = 0;
                double targetRotation = clockwise ? 360 : -360;
                await element.RotateTo(targetRotation, duration, Easing.Linear);
            }
        }

        private async Task TriggerCorePulseAsync()
        {
            await Task.WhenAll(
                CoreCenter.ScaleTo(1.4, 150, Easing.CubicOut),
                GlowEffect.ScaleTo(1.8, 150, Easing.CubicOut)
            );
            await Task.WhenAll(
                CoreCenter.ScaleTo(1.0, 200, Easing.CubicIn),
                GlowEffect.ScaleTo(1.0, 200, Easing.CubicIn)
            );
        }

        #endregion

        #region 2. OPERACIONES OFFLINE-FIRST Y SINCRONIZACIÓN

private async Task InitializeAndSyncAsync()
{
    // 1. Cargar SIEMPRE datos de SQLite primero (Funciona 100% offline)
    await LoadTasksFromLocalDbAsync();

    // 2. Reprogramar las notificaciones de tareas pendientes: al reabrir la app
    //    el SO pudo limpiar las programadas, y al estar en modo offline tampoco
    //    dependen del internet, así se aseguran en su tiempo establecido.
    await NotificadorTareas.ReprogramarRecordatoriosPendientesAsync();

    // 3. Verificar si realmente hay conexión antes de hablar con Supabase
    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
    {
        try
        {
            await _supabase.InitializeAsync();

            // Si el usuario entró con sesión offline, re-autenticar ahora que hay
            // conexión para que el token JWT quede activo y la IA funcione sin relogin.
            await ReautenticarSiNecesarioAsync();

            // Sincronizar en segundo plano
            _ = _syncService.SincronizarTareasAsync().ContinueWith(_ => 
            {
                MainThread.BeginInvokeOnMainThread(async () => await LoadTasksFromLocalDbAsync());
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error de red al conectar con Supabase: {ex.Message}");
        }
    }
    else
    {
        // Mensaje limpio para el usuario indicando el modo Offline
        AiMessageLabel.Text = "Modo fuera de línea activo. Operando localmente.";
    }
}

        /// <summary>
        /// Si hay conexión y el usuario inició sesión en modo offline (sin JWT),
        /// re-autentica contra Supabase para activar el token y permitir el uso
        /// de la IA y servicios online sin cerrar sesión ni volver a entrar.
        /// </summary>
        private async Task ReautenticarSiNecesarioAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserSession.CurrentJwt) &&
                    !string.IsNullOrEmpty(UserSession.OfflineEmail) &&
                    !string.IsNullOrEmpty(UserSession.OfflinePassword) &&
                    Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    var session = await _supabase.Auth.SignIn(UserSession.OfflineEmail, UserSession.OfflinePassword);
                    if (session?.User != null)
                    {
                        UserSession.CurrentJwt = session.AccessToken ?? "";
                        UserSession.CurrentAuthId = session.User.Id;
                        UserSession.OfflineEmail = string.Empty;
                        UserSession.OfflinePassword = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al re-autenticar tras recuperar conexión: {ex.Message}");
            }
        }

        private async Task LoadTasksFromLocalDbAsync()
        {
            int currentUserId = UserSession.CurrentUserId;
            if (currentUserId == 0)
            {
                AiMessageLabel.Text = "Sesión no detectada. Inicia sesión nuevamente.";
                return;
            }

            try
            {
                // Consulta LECTURA a SQLite Local
                var tasks = await _dbLocal.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == currentUserId && !t.IsDeleted)
                    .ToListAsync();

                var pendientes = tasks.Where(t => t.Estado != "Completado").ToList();
                var completadas = tasks.Where(t => t.Estado == "Completado").ToList();

                // Actualizar textos de los botones de filtro
                BtnFiltroPendientes.Text = $"Pendientes ({pendientes.Count})";
                BtnFiltroCompletadas.Text = $"Completadas ({completadas.Count})";

                // Mostrar según el filtro activo
                if (_filtroActual == "Pendientes")
                {
                    TasksCollectionView.ItemsSource = pendientes;
                    EmptyViewLabel.Text = "Sin tareas pendientes";
                    TareasCountLabel.Text = $"Mostrando: {pendientes.Count} pendiente(s)";
                }
                else
                {
                    TasksCollectionView.ItemsSource = completadas;
                    EmptyViewLabel.Text = "Sin tareas completadas";
                    TareasCountLabel.Text = $"Mostrando: {completadas.Count} completada(s)";
                }

                await TriggerCorePulseAsync();
                
                string userName = string.IsNullOrEmpty(UserSession.CurrentUserName) ? "Operador" : UserSession.CurrentUserName;
                AiMessageLabel.Text = $"Bienvenido {userName}. [Local] Tareas pendientes: {pendientes.Count}, completadas: {completadas.Count}";
            }
            catch (Exception ex)
            {
                AiMessageLabel.Text = $"Error local: {ex.Message}";
            }
        }

       private void OnAddTaskClicked(object sender, EventArgs e)
{
    int currentUserId = UserSession.CurrentUserId;
    if (currentUserId == 0) return;

    _tareaEditando = null;
    PopupHeaderLabel.Text = "Registrar Nueva Tarea";
    PopupTituloEntry.Text = string.Empty;
    PopupFechaPicker.Date = DateTime.Today.AddDays(2);
    PopupFrecuenciaPicker.SelectedIndex = 5;
    PopupBotonGuardar.Text = "✔ Guardar Tarea";
    PopupTituloEntry.Focus();
    TareaPopupOverlay.IsVisible = true;
}

private void OnTaskTapped(object sender, SelectionChangedEventArgs e)
{
    if (e.CurrentSelection?.FirstOrDefault() is TareaLocal tarea)
    {
        if (sender is CollectionView cv)
        {
            cv.SelectedItem = null;
        }
        EditarTareaAsync(tarea);
    }
}

private void EditarTareaAsync(TareaLocal tarea)
{
    int currentUserId = UserSession.CurrentUserId;
    if (currentUserId == 0) return;

    _tareaEditando = tarea;
    PopupHeaderLabel.Text = "Editar Tarea";
    PopupTituloEntry.Text = tarea.Titulo;
    PopupFechaPicker.Date = tarea.FechaVencimiento ?? DateTime.Today.AddDays(2);

    int horas = tarea.FrecuenciaRecordatorioHoras ?? 0;
    int idx = _opcionesFrecuencia.FindIndex(o => o.Key == horas);
    PopupFrecuenciaPicker.SelectedIndex = idx >= 0 ? idx : 0;

    PopupBotonGuardar.Text = "✔ Guardar Cambios";
    PopupTituloEntry.Focus();
    TareaPopupOverlay.IsVisible = true;
}

private void OnEditTaskClicked(object sender, EventArgs e)
{
    if ((sender as Button)?.BindingContext is TareaLocal tarea)
    {
        EditarTareaAsync(tarea);
    }
}

private async void OnToggleCompleteClicked(object sender, EventArgs e)
{
    if ((sender as Button)?.BindingContext is TareaLocal tarea)
    {
        tarea.Estado = (tarea.Estado == "Completado") ? "Pendiente" : "Completado";
        tarea.UltimaModificacion = DateTime.Now;
        tarea.IsSynced = false; // <-- marcar para re-sincronizar el cambio de estado
        await _dbLocal.UpdateAsync(tarea);

        if (tarea.Estado == "Completado")
        {
            // Al completar la tarea ya no hacen falta recordatorios
            NotificadorTareas.CancelarRecordatorio(tarea.IdLocal);
        }
        else
        {
            NotificadorTareas.ProgramarRecordatorio(tarea.IdLocal, tarea.Titulo, tarea.FechaVencimiento, tarea.FrecuenciaRecordatorioHoras ?? 0);
        }

        await LoadTasksFromLocalDbAsync();
        _ = _syncService.SincronizarTareasAsync();
    }
}

private async void OnDeleteTaskClicked(object sender, EventArgs e)
{
    if ((sender as Button)?.BindingContext is TareaLocal tarea)
    {
        bool confirmado = await DisplayAlertAsync("Eliminar tarea",
            $"¿Seguro que deseas eliminar la tarea '{tarea.Titulo}'?",
            "Eliminar", "Cancelar");
        if (!confirmado) return;

        // Borrado lógico: marcar como eliminada para que la sincronización la borre
        // de Supabase y de los otros dispositivos.
        tarea.IsDeleted = true;
        tarea.UltimaModificacion = DateTime.Now;
        await _dbLocal.UpdateAsync(tarea);

        // Cancelar recordatorios de la tarea
        NotificadorTareas.CancelarRecordatorio(tarea.IdLocal);

        await LoadTasksFromLocalDbAsync();
        _ = _syncService.SincronizarTareasAsync();
    }
}

private async void OnRefreshClicked(object sender, EventArgs e)
{
    // Botón de actualizar: recarga desde local y sincroniza con Supabase si hay internet
    await RefreshAllAsync();
}

private async void OnLogoutClicked(object sender, EventArgs e)
{
    bool confirmado = await DisplayAlertAsync("Cerrar sesión",
        "¿Seguro que deseas cerrar sesión?",
        "Cerrar sesión", "Cancelar");
    if (!confirmado) return;

    // Detener la sincronización automática y las animaciones
    OnDisappearing();

    // Limpiar la sesión activa y la guardada (para volver a pedir login)
    UserSession.LimpiarSesion();

    Application.Current.MainPage = new NavigationPage(new LoginPage());
}

private void OnFiltroPendientesClicked(object sender, EventArgs e)
{
    _filtroActual = "Pendientes";
    ActualizarEstiloBotonesFiltro();
    _ = LoadTasksFromLocalDbAsync();
}

private void OnFiltroCompletadasClicked(object sender, EventArgs e)
{
    _filtroActual = "Completadas";
    ActualizarEstiloBotonesFiltro();
    _ = LoadTasksFromLocalDbAsync();
}

private void ActualizarEstiloBotonesFiltro()
{
    if (_filtroActual == "Pendientes")
    {
        // Botón pendientes activo (resaltado)
        BtnFiltroPendientes.BackgroundColor = Color.FromArgb("#0E7490");
        BtnFiltroPendientes.BorderColor = Color.FromArgb("#67E8F9");
        BtnFiltroPendientes.TextColor = Color.FromArgb("#FFFFFF");
        BtnFiltroPendientes.BorderWidth = 2;
        BtnFiltroPendientes.FontAttributes = FontAttributes.Bold;
        ShadowPendientes.Opacity = 0.5F;

        // Botón completadas inactivo
        BtnFiltroCompletadas.BackgroundColor = Color.FromArgb("#111C30");
        BtnFiltroCompletadas.BorderColor = Color.FromArgb("#1E3A5F");
        BtnFiltroCompletadas.TextColor = Color.FromArgb("#94A3B8");
        BtnFiltroCompletadas.BorderWidth = 1;
        BtnFiltroCompletadas.FontAttributes = FontAttributes.None;
        ShadowCompletadas.Opacity = 0;
    }
    else
    {
        // Botón completadas activo (resaltado)
        BtnFiltroCompletadas.BackgroundColor = Color.FromArgb("#065F46");
        BtnFiltroCompletadas.BorderColor = Color.FromArgb("#6EE7B7");
        BtnFiltroCompletadas.TextColor = Color.FromArgb("#FFFFFF");
        BtnFiltroCompletadas.BorderWidth = 2;
        BtnFiltroCompletadas.FontAttributes = FontAttributes.Bold;
        ShadowCompletadas.Opacity = 0.5F;

        // Botón pendientes inactivo
        BtnFiltroPendientes.BackgroundColor = Color.FromArgb("#111C30");
        BtnFiltroPendientes.BorderColor = Color.FromArgb("#1E3A5F");
        BtnFiltroPendientes.TextColor = Color.FromArgb("#94A3B8");
        BtnFiltroPendientes.BorderWidth = 1;
        BtnFiltroPendientes.FontAttributes = FontAttributes.None;
        ShadowPendientes.Opacity = 0;
    }
}

private async Task RefreshAllAsync()
{
    try
    {
        AiMessageLabel.Text = "Actualizando...";

        // 1. Sincronizar con Supabase (sube pendientes y baja las del servidor)
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _supabase.InitializeAsync();
            await ReautenticarSiNecesarioAsync();
            await _syncService.SincronizarTareasAsync();
        }
        else
        {
            AiMessageLabel.Text = "Sin conexión. Mostrando datos locales.";
        }

        // 2. Recargar la lista desde SQLite local
        await LoadTasksFromLocalDbAsync();
    }
    catch (Exception ex)
    {
        await DisplayAlertAsync("Error", $"No se pudo actualizar: {ex.Message}", "OK");
        AiMessageLabel.Text = "Error al actualizar los datos.";
    }
}

        private async void OnGetSummaryClicked(object sender, EventArgs e)
        {
            await TriggerCorePulseAsync();
            int currentUserId = UserSession.CurrentUserId;

            try
            {
                // Consulta de resumen desde SQLite
                var tasks = await _dbLocal.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == currentUserId && !t.IsDeleted && t.Estado != "Completado")
                    .ToListAsync();

                var urgente = tasks.Where(t => t.FechaVencimiento.HasValue)
                    .OrderBy(t => t.FechaVencimiento)
                    .FirstOrDefault();

                if (urgente != null)
                {
                    AiMessageLabel.Text = $"Prioridad crítica: '{urgente.Titulo}' (Vence: {urgente.FechaVencimiento:dd/MM/yyyy}).";
                }
                else
                {
                    AiMessageLabel.Text = "Sin tareas pendientes en la base de datos local.";
                }
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Error al calcular el resumen local.";
            }
        }

        #endregion

        #region 3. CONSOLA DEL ASISTENTE IA

        // Etapas que el asistente va reportando mientras "piensa"
        private readonly (string Estado, string Detalle, string Log)[] _agenteEtapas = new[]
        {
            ("ENLACE",   "estableciendo sesión segura",     "> handshake con el agente..."),
            ("LECTURA",  "leyendo tu lista de tareas",     "> consultando tarea_local..."),
            ("NUBE",     "sincronizando con el servidor",  "> POST /functions/v1/analizar-tareas"),
            ("ANÁLISIS", "generando plan con IA",          "> modelo priorizando tus tareas...")
        };

        private async void OnAgenteClicked(object sender, EventArgs e)
        {
            if (UserSession.CurrentUserId == 0)
            {
                await DisplayAlertAsync("Sesión no detectada", "Inicia sesión nuevamente para usar el asistente.", "OK");
                return;
            }

            await AbrirConsolaAsync();
        }

        private async Task AbrirConsolaAsync()
        {
            // Guarda de reentrada: si ya hay una apertura o un cierre en curso,
            // ignoramos el toque en lugar de apilar dos consolas sobre la misma.
            if (_agenteOcupado || AgentePopupOverlay.IsVisible) return;

            _agenteOcupado = true;
            AgenteBoton.IsEnabled = false;

            try
            {
                PrepararConsola();

                AgentePopupOverlay.IsVisible = true;
                await AnimarEntradaAsync();

                IniciarAnimacionesAsistente();
                _ = EjecutarAnalisisAsync();
            }
            finally
            {
                _agenteOcupado = false;
            }
        }

        /// <summary>Deja la consola en su estado inicial para un nuevo análisis.</summary>
        private void PrepararConsola()
        {
            // Cada apertura invalida el análisis anterior en vuelo
            _agenteGeneracion++;

            _agenteVM.Reiniciar();

            AgenteContadorNumero.Text = "0";
            AgenteContadorTitulo.Text = "ESTADO DE TUS TAREAS";
            AgenteContadorDetalle.Text = "El agente está revisando tu lista completa.";

            AgenteProgresoBar.Progress = 0.15;
            AgenteProgresoLabel.Text = "Analizando tus tareas...";
            AgenteLogLabel.Text = string.Empty;
            AgenteErrorLabel.Text = string.Empty;

            AgentePensandoCard.IsVisible = true;
            AgentePensandoCard.Opacity = 1;
            AgenteVencidasCard.IsVisible = false;
            AgenteVencidasContador.Text = "0";
            AgenteVencidasAviso.Text = "Pasaron su fecha de vencimiento y siguen sin completarse.";
            AgenteContadorCard.IsVisible = false;
            AgentePlanCard.IsVisible = false;
            AgentePrioridadesCabecera.IsVisible = false;
            AgentePrioridadesStack.IsVisible = false;
            AgenteErrorCard.IsVisible = false;

            // Los tres puntos de "pensando" arrancan todos encendidos
            AgentePunto1.Opacity = 1;
            AgentePunto2.Opacity = 0.25;
            AgentePunto3.Opacity = 0.25;
            _agentePasoPuntos = 0;

            RestablecerOrbe();
        }

        private void RestablecerOrbe()
        {
            AgenteNucleo.Scale = 1;
            AgenteAnilloExt.Rotation = 0;
            AgenteAnilloInt.Rotation = 0;
            AgenteGlowOrb.Scale = 1;
            AgenteGlowOrb.Opacity = 0.3;
            AgenteStatusDot.Fill = new SolidColorBrush(Color.FromArgb("#34D399"));
            AgenteStatusDot.Opacity = 1;
        }

        #region 3.1 ANIMACIONES DE LA CONSOLA

        private async Task AnimarEntradaAsync()
        {
            AgenteConsolaCard.Opacity = 0;
            AgenteConsolaCard.Scale = 0.94;
            AgenteConsolaCard.TranslationY = 30;

            await Task.WhenAll(
                AgenteConsolaCard.FadeToAsync(1, 240, Easing.CubicOut),
                AgenteConsolaCard.ScaleToAsync(1, 280, Easing.CubicOut),
                AgenteConsolaCard.TranslateToAsync(0, 0, 280, Easing.CubicOut)
            );
        }

        private void IniciarAnimacionesAsistente()
        {
            // Cada apertura abre una sesión nueva. Los bucles de la sesión anterior
            // comparan el id al salir de cada vuelta y terminan para siempre; si solo
            // miraran un booleano, al reabrir volverían a activarse y se acumularían,
            // con varias animaciones compitiendo sobre los mismos elementos.
            _agenteSesion++;
            _agenteAnimando = true;

            int sesion = _agenteSesion;

            _ = RotarAgenteLoop(AgenteAnilloExt, 9000, true, sesion);
            _ = RotarAgenteLoop(AgenteAnilloInt, 5500, false, sesion);
            _ = PulsoNucleoAgenteLoop(sesion);

            IniciarRelojEtapas();
            IniciarRelojPuntos();
        }

        private bool SesionAgenteVigente(int sesion) => _agenteAnimando && sesion == _agenteSesion;

        private void DetenerAnimacionesAsistente()
        {
            _agenteAnimando = false;
            _agenteSesion++;

            if (_agenteRelojEtapas != null)
            {
                _agenteRelojEtapas.Stop();
                _agenteRelojEtapas.Tick -= OnEtapasTick;
                _agenteRelojEtapas = null;
            }

            if (_agenteRelojPuntos != null)
            {
                _agenteRelojPuntos.Stop();
                _agenteRelojPuntos.Tick -= OnPuntosTick;
                _agenteRelojPuntos = null;
            }
        }

        /// <summary>
        /// Giro continuo de un anillo. Al terminar la vuelta devuelve la rotación a 0
        /// (360° es visualmente idéntico a 0°, así que no se nota el salto).
        /// </summary>
        private async Task RotarAgenteLoop(VisualElement element, uint duration, bool clockwise, int sesion)
        {
            double paso = clockwise ? 360 : -360;

            while (SesionAgenteVigente(sesion))
            {
                await element.RotateToAsync(paso, duration, Easing.Linear);

                // Solo se recentra si esta sesión sigue viva: si no, el próximo
                // arranque ya lo deja en 0 con RestablecerOrbe().
                if (SesionAgenteVigente(sesion)) element.Rotation = 0;
            }
        }

        private async Task PulsoNucleoAgenteLoop(int sesion)
        {
            while (SesionAgenteVigente(sesion))
            {
                await Task.WhenAll(
                    AgenteNucleo.ScaleToAsync(1.18, 900, Easing.SinInOut),
                    AgenteGlowOrb.ScaleToAsync(1.25, 900, Easing.SinInOut),
                    AgenteGlowOrb.FadeToAsync(0.55, 900, Easing.SinInOut)
                );

                if (!SesionAgenteVigente(sesion)) break;

                await Task.WhenAll(
                    AgenteNucleo.ScaleToAsync(1.0, 900, Easing.SinInOut),
                    AgenteGlowOrb.ScaleToAsync(1.0, 900, Easing.SinInOut),
                    AgenteGlowOrb.FadeToAsync(0.3, 900, Easing.SinInOut)
                );
            }
        }

        /// <summary>Punto de estado: cambia de color y parpadea al entrar en una etapa.</summary>
        private async Task ParpadeoStatusDotAsync(string hex)
        {
            AgenteStatusDot.Fill = new SolidColorBrush(Color.FromArgb(hex));

            await Task.WhenAll(
                AgenteStatusDot.FadeToAsync(0.25, 180, Easing.CubicOut),
                AgenteStatusDot.ScaleToAsync(1.35, 180, Easing.CubicOut)
            );

            await Task.WhenAll(
                AgenteStatusDot.FadeToAsync(1, 260, Easing.CubicOut),
                AgenteStatusDot.ScaleToAsync(1, 260, Easing.CubicOut)
            );
        }

        private async Task RevelarAsync(VisualElement element, uint duracion = 280)
        {
            element.IsVisible = true;
            element.Opacity = 0;
            element.TranslationY = 20;

            await Task.WhenAll(
                element.FadeToAsync(1, duracion, Easing.CubicOut),
                element.TranslateToAsync(0, 0, duracion, Easing.CubicOut)
            );
        }

        private async Task OcultarAsync(VisualElement element, uint duracion = 200)
        {
            await element.FadeToAsync(0, duracion, Easing.CubicIn);
            element.IsVisible = false;
        }

        #endregion

        #region 3.2 CICLO DE ESTADOS Y ANÁLISIS

        private void IniciarRelojEtapas()
        {
            _agenteEtapa = 0;
            AplicarEtapa(0);

            _agenteRelojEtapas = Dispatcher.CreateTimer();
            _agenteRelojEtapas.Interval = TimeSpan.FromMilliseconds(1100);
            _agenteRelojEtapas.Tick += OnEtapasTick;
            _agenteRelojEtapas.Start();
        }

        private void OnEtapasTick(object? sender, EventArgs e)
        {
            if (_agenteEtapa >= _agenteEtapas.Length - 1)
            {
                _agenteRelojEtapas?.Stop();
                return;
            }

            _agenteEtapa++;
            AplicarEtapa(_agenteEtapa);
        }

        private void AplicarEtapa(int indice)
        {
            var etapa = _agenteEtapas[indice];

            AgenteEstadoLabel.Text = etapa.Estado;
            AgenteSubEstadoLabel.Text = etapa.Detalle;
            AgenteProgresoBar.Progress = Math.Min(0.9, 0.15 + indice * 0.2);

            string salto = indice == 0 ? string.Empty : AgenteLogLabel.Text + "\n";
            AgenteLogLabel.Text = salto + etapa.Log;

            _ = ParpadeoStatusDotAsync(etapa.Estado == "ANÁLISIS" ? "#22D3EE" : "#A78BFA");
        }

        private void IniciarRelojPuntos()
        {
            _agenteRelojPuntos = Dispatcher.CreateTimer();
            _agenteRelojPuntos.Interval = TimeSpan.FromMilliseconds(340);
            _agenteRelojPuntos.Tick += OnPuntosTick;
            _agenteRelojPuntos.Start();
        }

        private void OnPuntosTick(object? sender, EventArgs e)
        {
            Microsoft.Maui.Controls.Shapes.Ellipse[] puntos = { AgentePunto1, AgentePunto2, AgentePunto3 };

            for (int i = 0; i < puntos.Length; i++)
            {
                int desfase = (_agentePasoPuntos + i) % puntos.Length;
                puntos[i].Opacity = desfase == 0 ? 1.0 : 0.25;
            }

            _agentePasoPuntos++;
        }

        private async Task EjecutarAnalisisAsync()
        {
            int generacion = _agenteGeneracion;

            try
            {
                // Asegurar que las ediciones locales estén subidas antes de analizar
                await RefreshAllAsync();

                // Detectar vencidas en local: es instantáneo y funciona sin conexión
                await CargarVencidasAsync(generacion);

                if (generacion != _agenteGeneracion) return;

                var resultado = await _agenteIA.AnalizarTareasAsync();

                if (resultado == null)
                {
                    throw new InvalidOperationException("El agente no devolvió respuesta. Vuelve a intentarlo en un momento.");
                }

                // El usuario pidió otro análisis mientras este esperaba: descartar
                if (generacion != _agenteGeneracion) return;

                if (_agenteRelojEtapas != null) _agenteRelojEtapas.Stop();
                if (_agenteRelojPuntos != null) _agenteRelojPuntos.Stop();

                await MostrarResultadoAsync(resultado);
            }
            catch (Exception ex)
            {
                if (generacion != _agenteGeneracion) return;

                if (_agenteRelojEtapas != null) _agenteRelojEtapas.Stop();
                if (_agenteRelojPuntos != null) _agenteRelojPuntos.Stop();

                MostrarError(ex.Message);
            }
            finally
            {
                if (generacion == _agenteGeneracion)
                {
                    _agenteVM.EstaCargando = false;
                    AgenteBoton.IsEnabled = true;
                }
            }
        }

        private async Task MostrarResultadoAsync(AgenteIAServicio.ResultadoAgente resultado)
        {
            string nombre = string.IsNullOrWhiteSpace(resultado.Nombre) ? "Operador" : resultado.Nombre;
            int pendientes = resultado.Pendientes;
            var prioridades = resultado.Prioridades ?? new List<AgenteIAServicio.Prioridad>();

            // 1. Saludo personalizado en la burbuja principal
            int vencidas = _agenteVM.Vencidas.Count;

            _agenteVM.Saludo = vencidas > 0
                ? $"Hola {nombre}, atendé esto primero: tenés tareas vencidas."
                : $"Hola {nombre}, ya analicé tus tareas.";

            if (vencidas > 0)
            {
                _agenteVM.SaludoDetalle = $"{vencidas} tarea(s) pasaron su fecha y siguen sin completarse. Marcadas como NO TERMINADAS más abajo.";
            }
            else
            {
                _agenteVM.SaludoDetalle = pendientes == 0
                    ? "No tienes nada pendiente. Todo tu plan de trabajo está al día."
                    : $"Tienes {pendientes} tarea(s) en la cola, ninguna vencida. Este es el plan que armé para ti.";
            }

            AgenteEstadoLabel.Text = "ANÁLISIS COMPLETO";
            AgenteSubEstadoLabel.Text = vencidas > 0
                ? $"{vencidas} vencida(s) · {pendientes} pendientes · {prioridades.Count} prioridad(es)"
                : $"{pendientes} pendientes · {prioridades.Count} prioridad(es)";
            AgenteProgresoBar.Progress = 1.0;
            AgenteProgresoLabel.Text = "Análisis completado.";
            AgenteLogLabel.Text += "\n> respuesta recibida del agente.";
            _ = ParpadeoStatusDotAsync("#34D399");

            await Task.Delay(260);

            // 2. Cerrar la tarjeta de "pensando"
            await OcultarAsync(AgentePensandoCard);

            // 3. Contador con cuenta ascendente
            _agenteVM.Pendientes = pendientes;
            AgenteContadorDetalle.Text = vencidas > 0
                ? $"{vencidas} de {pendientes} ya están vencidas y siguen sin completarse."
                : pendientes == 0
                    ? "Sin tareas en la cola. Buen trabajo."
                    : $"El agente revisó {pendientes} tarea(s) y armó este orden de trabajo.";

            await RevelarAsync(AgenteContadorCard);
            await AnimarContadorAsync(pendientes);

            // 4. Plan de acción
            string plan = string.IsNullOrWhiteSpace(resultado.Plan)
                ? "No pude generar un plan esta vez. Intenta de nuevo en un momento."
                : resultado.Plan;

            _agenteVM.Plan = plan;
            await RevelarAsync(AgentePlanCard);
            await Task.Delay(80);

            // 5. Prioridades en tarjetas
            if (prioridades.Count > 0)
            {
                await RevelarAsync(AgentePrioridadesCabecera, 220);

                int posicion = 0;
                foreach (var p in prioridades)
                {
                    posicion++;
                    _agenteVM.Prioridades.Add(new PrioridadViewModel
                    {
                        Numero = posicion.ToString("00"),
                        Titulo = string.IsNullOrWhiteSpace(p.Titulo) ? "(tarea sin título)" : p.Titulo,
                        Razon = string.IsNullOrWhiteSpace(p.Razon) ? "Sin detalle" : p.Razon
                    });
                }

                await RevelarAsync(AgentePrioridadesStack, 320);
            }

            // 6. Llevar la vista al final del reporte
            await Task.Delay(150);
            await ScrollToFinalAsync();
        }

        /// <summary>
        /// Arma la sección "TAREAS VENCIDAS": tareas cuya fecha de vencimiento ya pasó
        /// y que siguen sin completarse. Se calcula contra SQLite local para que la
        /// sección exista aunque el agente no responda o no haya internet.
        /// </summary>
        private async Task CargarVencidasAsync(int generacion)
        {
            try
            {
                int usuario = UserSession.CurrentUserId;
                DateTime hoy = DateTime.Today;

                var propias = await _dbLocal.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == usuario && !t.IsDeleted)
                    .ToListAsync();

                // El filtrado por fecha va en memoria: sqlite-net no traduce la
                // comparación de un DateTime? dentro del Where.
                var vencidas = propias
                    .Where(t => t.Estado != "Completado"
                             && t.FechaVencimiento.HasValue
                             && t.FechaVencimiento.Value.Date < hoy)
                    .OrderBy(t => t.FechaVencimiento)
                    .ToList();

                if (generacion != _agenteGeneracion) return;

                _agenteVM.Vencidas.Clear();

                int posicion = 0;
                foreach (var tarea in vencidas)
                {
                    posicion++;

                    DateTime vencimiento = tarea.FechaVencimiento!.Value;
                    int dias = (hoy - vencimiento.Date).Days;

                    string retraso = dias switch
                    {
                        0 => "vence hoy",
                        1 => "hace 1 día",
                        _ => $"hace {dias} días"
                    };

                    _agenteVM.Vencidas.Add(new TareaVencidaViewModel(
                        posicion.ToString("00"),
                        string.IsNullOrWhiteSpace(tarea.Titulo) ? "(tarea sin título)" : tarea.Titulo,
                        $"Venció el {vencimiento:dd/MM/yyyy} · {retraso}"));
                }

                if (_agenteVM.Vencidas.Count == 0) return;

                int total = _agenteVM.Vencidas.Count;
                AgenteVencidasContador.Text = total.ToString();
                AgenteVencidasAviso.Text = total == 1
                    ? "1 tarea pasó su fecha de vencimiento y sigue sin completarse."
                    : $"{total} tareas pasaron su fecha de vencimiento y siguen sin completarse.";

                AgenteLogLabel.Text += $"\n> {total} tarea(s) vencida(s) detectada(s).";

                await RevelarAsync(AgenteVencidasCard, 320);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudieron leer las tareas vencidas: {ex.Message}");
            }
        }

        private void MostrarError(string mensaje)
        {
            _agenteVM.EstaCargando = false;
            _agenteVM.TieneError = true;
            _agenteVM.MensajeError = mensaje;

            AgenteErrorLabel.Text = mensaje;
            AgenteErrorCard.IsVisible = true;
            AgenteProgresoLabel.Text = "No pude completar el análisis.";

            AgenteEstadoLabel.Text = "ENLACE FALLIDO";
            AgenteSubEstadoLabel.Text = "revisando la conexión...";
            _ = ParpadeoStatusDotAsync("#F87171");
        }

        private async Task AnimarContadorAsync(int valor)
        {
            const int pasos = 20;

            for (int i = 1; i <= pasos; i++)
            {
                if (!AgentePopupOverlay.IsVisible) return;

                int mostrado = (int)Math.Round(valor * (i / (double)pasos));
                AgenteContadorNumero.Text = mostrado.ToString();
                await Task.Delay(20);
            }

            AgenteContadorNumero.Text = valor.ToString();
        }

        private async Task ScrollToFinalAsync()
        {
            try
            {
                await AgenteScrollView.ScrollToAsync(0, 100000, animated: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo desplazar la consola del asistente: {ex.Message}");
            }
        }

        #endregion

        #region 3.3 CIERRE Y ACCIONES DE LA CONSOLA

        private async void OnAgentePopupCerrarClicked(object sender, EventArgs e)
        {
            await CerrarConsolaAsync();
        }

        private async void OnAgenteReanalizarClicked(object sender, EventArgs e)
        {
            if (_agenteOcupado) return;

            await CerrarConsolaAsync();
            await Task.Delay(220);
            await AbrirConsolaAsync();
        }

        private async Task CerrarConsolaAsync()
        {
            if (!AgentePopupOverlay.IsVisible || _agenteOcupado) return;

            _agenteOcupado = true;

            try
            {
                DetenerAnimacionesAsistente();

                await Task.WhenAll(
                    AgenteConsolaCard.FadeToAsync(0, 190, Easing.CubicIn),
                    AgenteConsolaCard.ScaleToAsync(0.95, 190, Easing.CubicIn),
                    AgenteConsolaCard.TranslateToAsync(0, 20, 190, Easing.CubicIn)
                );

                AgenteConsolaCard.Opacity = 1;
                AgenteConsolaCard.Scale = 1;
                AgenteConsolaCard.TranslationY = 0;

                AgentePopupOverlay.IsVisible = false;
                AgenteBoton.IsEnabled = true;
            }
            finally
            {
                _agenteOcupado = false;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (AgentePopupOverlay.IsVisible)
            {
                _ = CerrarConsolaAsync();
                return true;
            }

            return base.OnBackButtonPressed();
        }

        #endregion

        #endregion

        #region 4. POPUP NUEVA TAREA / EDITAR TAREA

        private async void OnPopupGuardarClicked(object sender, EventArgs e)
        {
            string titulo = PopupTituloEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(titulo))
            {
                await DisplayAlertAsync("Tarea sin título", "Escribe un título para la tarea.", "OK");
                return;
            }

            int horas = 0;
            if (PopupFrecuenciaPicker.SelectedIndex >= 0 && PopupFrecuenciaPicker.SelectedIndex < _opcionesFrecuencia.Count)
            {
                horas = _opcionesFrecuencia[PopupFrecuenciaPicker.SelectedIndex].Key;
            }

            if (_tareaEditando == null)
            {
                var nuevaTareaLocal = new TareaLocal
                {
                    IdUsuario = UserSession.CurrentUserId,
                    Titulo = titulo,
                    Descripcion = "Registrada desde la app",
                    FechaVencimiento = PopupFechaPicker.Date,
                    FrecuenciaRecordatorioHoras = horas,
                    Estado = "Pendiente",
                    IsSynced = false,
                    UltimaModificacion = DateTime.Now
                };

                await _dbLocal.InsertAsync(nuevaTareaLocal);
                NotificadorTareas.ProgramarRecordatorio(nuevaTareaLocal.IdLocal, nuevaTareaLocal.Titulo, nuevaTareaLocal.FechaVencimiento, nuevaTareaLocal.FrecuenciaRecordatorioHoras ?? 0);
                _ = _syncService.SincronizarTareasAsync();
                EnviarNotificacionPC("✔ Tarea Creada", $"'{titulo}' se guardó correctamente.");
            }
            else
            {
                _tareaEditando.Titulo = titulo;
                _tareaEditando.FechaVencimiento = PopupFechaPicker.Date;
                _tareaEditando.FrecuenciaRecordatorioHoras = horas;
                _tareaEditando.UltimaModificacion = DateTime.Now;
                _tareaEditando.IsSynced = false; // <-- marcar para re-sincronizar la edición

                await _dbLocal.UpdateAsync(_tareaEditando);
                NotificadorTareas.CancelarRecordatorio(_tareaEditando.IdLocal);
                NotificadorTareas.ProgramarRecordatorio(_tareaEditando.IdLocal, _tareaEditando.Titulo, _tareaEditando.FechaVencimiento, _tareaEditando.FrecuenciaRecordatorioHoras ?? 0);
                _ = _syncService.SincronizarTareasAsync();
                EnviarNotificacionPC("✏️ Tarea Actualizada", $"'{titulo}' se editó correctamente.");
            }

            TareaPopupOverlay.IsVisible = false;
            await LoadTasksFromLocalDbAsync();
        }

        private void OnPopupCancelarClicked(object sender, EventArgs e)
        {
            TareaPopupOverlay.IsVisible = false;
        }

        #endregion
    }
}