namespace Asistente
{
    public class DatosNuevaTarea
    {
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaVencimiento { get; set; }
        public int FrecuenciaRecordatorioHoras { get; set; } = 0; // 0 = sin recordatorio
    }

    public partial class NuevaTareaPage : ContentPage
    {
        // Permite que la página que la abrió espere el resultado de forma segura
        private readonly TaskCompletionSource<DatosNuevaTarea> _tcs = new();
        public Task<DatosNuevaTarea> ResultadoTask => _tcs.Task;

        // Opciones de frecuencia en horas (aparecen en el mismo orden en el Picker)
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

        public NuevaTareaPage()
        {
            InitializeComponent();

            // Dar opciones al Picker de frecuencia
            foreach (var opcion in _opcionesFrecuencia)
            {
                FrecuenciaPicker.Items.Add(opcion.Value);
            }
            // Valor por defecto: "Cada 4 horas" (índice 5, horas = 4)
            FrecuenciaPicker.SelectedIndex = 5;

            // Fechas por defecto: hoy o mañana
            DateTime hoy = DateTime.Today;
            FechaVencimientoPicker.MinimumDate = hoy;
            FechaVencimientoPicker.MaximumDate = hoy.AddYears(5);
            FechaVencimientoPicker.Date = hoy.AddDays(2);
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            string titulo = TituloEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(titulo))
            {
                await DisplayAlertAsync("Tarea sin título", "Escribe un título para la tarea.", "OK");
                return;
            }

            int horas = 0;
            if (FrecuenciaPicker.SelectedIndex >= 0 && FrecuenciaPicker.SelectedIndex < _opcionesFrecuencia.Count)
            {
                horas = _opcionesFrecuencia[FrecuenciaPicker.SelectedIndex].Key;
            }

            var resultado = new DatosNuevaTarea
            {
                Titulo = titulo,
                FechaVencimiento = FechaVencimientoPicker.Date ?? DateTime.Today,
                FrecuenciaRecordatorioHoras = horas
            };

            _tcs.TrySetResult(resultado);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            // Devolver null para indicar que se canceló (sin disparar nada más)
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}
