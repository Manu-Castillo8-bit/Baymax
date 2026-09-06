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
        
        // Referencias a la base de datos local y al servicio de sincronización
        private SQLiteAsyncConnection _dbLocal;
        private SyncService _syncService;

        // Temporizador para sincronización automática periódica
        private IDispatcherTimer _syncTimer;
        private bool _isSyncing = false;

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

            // 1. Inicializar la ruta local de SQLite
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "asistente.db3");
            _dbLocal = new SQLiteAsyncConnection(dbPath);
            
            // 2. Inicializar el servicio de sincronización
            _syncService = new SyncService(dbPath, _supabase);
        }

        private void OnTestNotificationClicked(object sender, EventArgs e)
{
    try
    {
        // Programar notificación a los 5 segundos
        var request = new NotificationRequest
        {
            NotificationId = 9999, // ID fijo de prueba
            Title = "🧪 Prueba Exitosa",
            Description = "¡Las notificaciones están funcionando correctamente en tu dispositivo!",
            BadgeNumber = 1,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTimeOffset.Now.AddSeconds(5) // Llegará en 5 segundos
            }
        };

        LocalNotificationCenter.Current.Show(request);

        // Aviso visual para el usuario
        DisplayAlert("Prueba en marcha", "La notificación se disparará en 5 segundos. Puedes minimizar la app.", "OK");
    }
    catch (Exception ex)
    {
        DisplayAlert("Error de Notificación", ex.Message, "OK");
    }
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
        
private void ProgramarNotificacionRecordatorio(int idTarea, string titulo, DateTime? fechaVencimiento, int frecuenciaHoras)
{
    try
    {
        var request = new NotificationRequest
        {
            NotificationId = idTarea, // ID local de la tarea para evitar duplicados y poder cancelarla
            Title = "⏰ Recordatorio: Tarea Próxima",
            Description = $"La tarea: '{titulo}' está pendiente.",
            BadgeNumber = 1,
            Schedule = new NotificationRequestSchedule()
        };

        if (frecuenciaHoras > 0)
        {
            // Modo recurrente: recordar cada X horas hasta el vencimiento
            request.Schedule.NotifyTime = DateTimeOffset.Now.AddSeconds(5); // primera notificación casi inmediata
            request.Schedule.RepeatType = NotificationRepeat.TimeInterval;
            request.Schedule.NotifyRepeatInterval = TimeSpan.FromHours(frecuenciaHoras);
        }
        else if (fechaVencimiento.HasValue)
        {
            // Modo simple: avisar 1 día antes del vencimiento (sin repetición)
            DateTime fechaNotificacion = fechaVencimiento.Value.AddDays(-1);
            if (fechaNotificacion <= DateTime.Now) return;

            request.Schedule.NotifyTime = new DateTimeOffset(fechaNotificacion);
            request.Schedule.RepeatType = NotificationRepeat.No;
        }
        else
        {
            return; // sin fecha y sin frecuencia: no hay nada que programar
        }

        LocalNotificationCenter.Current.Show(request);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error al programar notificación: {ex.Message}");
    }
}

private void EnviarNotificacionPC(string titulo, string mensaje)
{
    try
    {
        var request = new NotificationRequest
        {
            NotificationId = new Random().Next(1000, 9999),
            Title = titulo,
            Description = mensaje,
            BadgeNumber = 1,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTimeOffset.Now.AddSeconds(1) // Se dispara de inmediato
            }
        };

        LocalNotificationCenter.Current.Show(request);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error al enviar notificación: {ex.Message}");
    }
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

    // 2. Verificar si realmente hay conexión antes de hablar con Supabase
    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
    {
        try
        {
            await _supabase.InitializeAsync();
            
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

                TasksCollectionView.ItemsSource = tasks;
                int pendingCount = tasks.Count(t => t.Estado != "Completado");

                await TriggerCorePulseAsync();
                
                string userName = string.IsNullOrEmpty(UserSession.CurrentUserName) ? "Operador" : UserSession.CurrentUserName;
                AiMessageLabel.Text = $"Bienvenido {userName}. [Local] Tareas pendientes: {pendingCount}";
            }
            catch (Exception ex)
            {
                AiMessageLabel.Text = $"Error local: {ex.Message}";
            }
        }

       private async void OnAddTaskClicked(object sender, EventArgs e)
{
    int currentUserId = UserSession.CurrentUserId;
    if (currentUserId == 0) return;

    // Abrir la pantalla de registro de tarea (fecha de vencimiento + frecuencia de recordatorio)
    // Usamos TaskCompletionSource: la página modal no se comunica vía eventos asíncronos que
    // puedan congelar la UI; simplemente esperamos su resultado.
    var pagina = new NuevaTareaPage();
    await Navigation.PushModalAsync(pagina);

    var datos = await pagina.ResultadoTask;
    if (datos == null) return; // El usuario canceló

    var nuevaTareaLocal = new TareaLocal
    {
        IdUsuario = currentUserId,
        Titulo = datos.Titulo,
        Descripcion = "Registrada desde la app",
        FechaVencimiento = datos.FechaVencimiento,
        FrecuenciaRecordatorioHoras = datos.FrecuenciaRecordatorioHoras,
        Estado = "Pendiente",
        IsSynced = false,
        UltimaModificacion = DateTime.Now
    };

    // 1. Guardar en la base de datos local (SQLite genera autoincremental el Id)
    await _dbLocal.InsertAsync(nuevaTareaLocal);

    // 2. Programar las notificaciones de recordatorio con la frecuencia elegida
    ProgramarNotificacionRecordatorio(nuevaTareaLocal.IdLocal, nuevaTareaLocal.Titulo, nuevaTareaLocal.FechaVencimiento, nuevaTareaLocal.FrecuenciaRecordatorioHoras ?? 0);

    // 3. Actualizar la vista
    await LoadTasksFromLocalDbAsync();
    _ = _syncService.SincronizarTareasAsync();
}

private async void OnRefreshClicked(object sender, EventArgs e)
{
    // Botón de actualizar: recarga desde local y sincroniza con Supabase si hay internet
    await RefreshAllAsync();
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
    }
}