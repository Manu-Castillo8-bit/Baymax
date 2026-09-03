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

            string url = "https://tu-proyecto.supabase.co";
            string key = "tu-anon-key";

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

        #region ANIMACIONES DEL NÚCLEO HUD

        private async void StartHudAnimations()
        {
            // Ejecutar rotaciones continuas e independientes
            _ = RotateElementLoop(OuterRing1, 12000, true);   // Giro lento horario
            _ = RotateElementLoop(OuterRing2, 8000, false);   // Giro medio antihorario
            _ = RotateElementLoop(MidRing, 15000, true);      // Giro muy lento horario
            _ = RotateElementLoop(InnerRing, 4000, false);    // Giro rápido antihorario

            // Bucle de pulso del núcleo central
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

        #region CONEXIÓN CON SUPABASE

        private async Task InitializeSupabaseAndLoadTasksAsync()
        {
            try
            {
                await _supabase.InitializeAsync();
                await LoadTasksAsync();
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Modo fuera de línea. No se pudo conectar al servidor.";
            }
        }

        private async Task LoadTasksAsync()
        {
            try
            {
                var response = await _supabase.From<TodoTask>().Get();
                var tasks = response.Models;

                TasksCollectionView.ItemsSource = tasks;
                int pendingCount = tasks.Count(t => !t.IsCompleted);

                await TriggerCorePulseAsync();
                AiMessageLabel.Text = $"Estado del sistema: {pendingCount} tareas pendientes detectadas.";
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Error al sincronizar datos desde Supabase.";
            }
        }

        private async void OnAddTaskClicked(object sender, EventArgs e)
        {
            string title = await DisplayPromptAsync("Nueva Tarea", "¿Qué deseas registrar?");
            if (string.IsNullOrWhiteSpace(title)) return;

            var newTask = new TodoTask
            {
                Title = title,
                DueDate = DateTime.Now.AddHours(4),
                IsCompleted = false
            };

            await _supabase.From<TodoTask>().Insert(newTask);
            await LoadTasksAsync();
        }

        private async void OnGetSummaryClicked(object sender, EventArgs e)
        {
            await TriggerCorePulseAsync();

            try
            {
                var response = await _supabase.From<TodoTask>().Get();
                var urgentTask = response.Models
                    .Where(t => !t.IsCompleted)
                    .OrderBy(t => t.DueDate)
                    .FirstOrDefault();

                if (urgentTask != null)
                {
                    AiMessageLabel.Text = $"Prioridad crítica: '{urgentTask.Title}' (Vence: {urgentTask.DueDate:HH:mm}).";
                }
                else
                {
                    AiMessageLabel.Text = "Sin tareas pendientes. Todos los módulos operativos al 100%.";
                }
            }
            catch (Exception)
            {
                AiMessageLabel.Text = "Error al consultar la prioridad.";
            }
        }

        #endregion
    }

    [Table("tasks")]
    public class TodoTask : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("due_date")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(1);

        [Column("is_completed")]
        public bool IsCompleted { get; set; } = false;

        [Column("priority")]
        public int Priority { get; set; } = 2;
    }
}