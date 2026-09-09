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

        private IDispatcherTimer _syncTimer;
        private bool _isSyncing = false;

        // Popup: tarea que se está editando (null = nueva)
        private TareaLocal _tareaEditando;

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

            // Inicializar opciones del picker de frecuencia del popup
            foreach (var opcion in _opcionesFrecuencia)
            {
                PopupFrecuenciaPicker.Items.Add(opcion.Value);
            }

            DateTime hoy = DateTime.Today;
            PopupFechaPicker.MinimumDate = hoy;
            PopupFechaPicker.MaximumDate = hoy.AddYears(5);
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

                var pendientes = tasks.Where(t => t.Estado != "Completado").ToList();
                var completadas = tasks.Where(t => t.Estado == "Completado").ToList();

                PendingTasksCollectionView.ItemsSource = pendientes;
                CompletedTasksCollectionView.ItemsSource = completadas;

                PendingCountLabel.Text = $"Pendientes ({pendientes.Count})";
                CompletedCountLabel.Text = $"Completadas ({completadas.Count})";

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

        private async void OnAgenteClicked(object sender, EventArgs e)
        {
            if (UserSession.CurrentUserId == 0)
            {
                await DisplayAlertAsync("Sesión no detectada", "Inicia sesión nuevamente para usar el asistente.", "OK");
                return;
            }

            // Prevenir doble toque mientras trabaja
            AgenteBoton.IsEnabled = false;

            AgentePopupOverlay.IsVisible = true;
            AgenteResultadoLabel.Text = "Analizando tus tareas...";
            AgenteActivityIndicator.IsRunning = true;
            AgenteActivityIndicator.IsVisible = true;

            try
            {
                // Asegurar que la edición de fechas se suba a Supabase antes de analizar
                await RefreshAllAsync();

                var resultado = await _agenteIA.AnalizarTareasAsync();

                if (resultado == null)
                {
                    AgenteResultadoLabel.Text = "No obtuve respuesta del agente. Intenta nuevamente.";
                    return;
                }

                var texto = new System.Text.StringBuilder();
                texto.AppendLine($"Hola {resultado.Nombre ?? "Operador"}. Tienes {resultado.Pendientes} tareas pendientes.");
                texto.AppendLine();
                texto.AppendLine("📋 PLAN:");
                texto.AppendLine(resultado.Plan ?? "Sin plan.");

                if (resultado.Prioridades is { Count: > 0 })
                {
                    texto.AppendLine();
                    texto.AppendLine("🎯 PRIORIDADES:");
                    foreach (var p in resultado.Prioridades)
                    {
                        texto.AppendLine($"• {p.Titulo}");
                        if (!string.IsNullOrWhiteSpace(p.Razon))
                        {
                            texto.AppendLine($"   ↳ {p.Razon}");
                        }
                    }
                }

                AgenteResultadoLabel.Text = texto.ToString();
            }
            catch (Exception ex)
            {
                AgenteResultadoLabel.Text = $"No pude contactar al agente:\n{ex.Message}";
            }
            finally
            {
                AgenteActivityIndicator.IsRunning = false;
                AgenteActivityIndicator.IsVisible = false;
                AgenteBoton.IsEnabled = true;
            }
        }

        private void OnAgentePopupCerrarClicked(object sender, EventArgs e)
        {
            AgentePopupOverlay.IsVisible = false;
        }

        #endregion

        #region 3. POPUP NUEVA TAREA / EDITAR TAREA

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