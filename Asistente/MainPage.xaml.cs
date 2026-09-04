using Supabase;
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
}