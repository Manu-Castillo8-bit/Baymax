using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Asistente
{
    /// <summary>
    /// Una prioridad mostrada dentro de la consola del asistente (tarjeta con ranking).
    /// </summary>
    public class PrioridadViewModel : INotifyPropertyChanged
    {
        private string _numero = string.Empty;
        private string _titulo = string.Empty;
        private string _razon = string.Empty;

        /// <summary>Ranking formateado ("01", "02", ...).</summary>
        public string Numero
        {
            get => _numero;
            set => Asignar(ref _numero, value);
        }

        public string Titulo
        {
            get => _titulo;
            set => Asignar(ref _titulo, value);
        }

        /// <summary>Motivo por el que el agente considera prioritaria la tarea.</summary>
        public string Razon
        {
            get => _razon;
            set => Asignar(ref _razon, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Asignar(ref string campo, string valor, [CallerMemberName] string? propiedad = null)
        {
            if (campo == valor) return;
            campo = valor;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }

    /// <summary>
    /// Tarea que ya pasó su fecha de vencimiento y sigue sin completarse.
    /// Se muestra como "no terminada" en la consola del asistente.
    /// </summary>
    public class TareaVencidaViewModel
    {
        /// <summary>Posición en el listado de vencidas ("01", "02", ...).</summary>
        public string Numero { get; }

        public string Titulo { get; }

        /// <summary>Fecha de vencimiento y días de retraso, p. ej. "Venció el 12/03/2026 · hace 3 días".</summary>
        public string Detalle { get; }

        public TareaVencidaViewModel(string numero, string titulo, string detalle)
        {
            Numero = numero;
            Titulo = titulo;
            Detalle = detalle;
        }
    }

    /// <summary>
    /// Estado y contenido de la consola del Asistente IA. Alimenta el overlay
    /// de MainPage para poder enlazar tarjetas sin llenarlas a mano.
    /// </summary>
    public class AgenteViewModel : INotifyPropertyChanged
    {
        private string _saludo = "Iniciando el núcleo del asistente...";
        private string _saludoDetalle = string.Empty;
        private string _plan = string.Empty;
        private int _pendientes;
        private bool _estaCargando = true;
        private bool _tieneError;
        private string _mensajeError = string.Empty;

        /// <summary>Saludo del asistente dentro de la burbuja principal.</summary>
        public string Saludo
        {
            get => _saludo;
            set => Asignar(ref _saludo, value);
        }

        /// <summary>Línea secundaria de la burbuja de saludo.</summary>
        public string SaludoDetalle
        {
            get => _saludoDetalle;
            set => Asignar(ref _saludoDetalle, value);
        }

        /// <summary>Plan de acción devuelto por el agente.</summary>
        public string Plan
        {
            get => _plan;
            set { Asignar(ref _plan, value); OnPropertyChanged(nameof(TienePlan)); }
        }

        public int Pendientes
        {
            get => _pendientes;
            set => Asignar(ref _pendientes, value);
        }

        public bool EstaCargando
        {
            get => _estaCargando;
            set => Asignar(ref _estaCargando, value);
        }

        public bool TieneError
        {
            get => _tieneError;
            set => Asignar(ref _tieneError, value);
        }

        public string MensajeError
        {
            get => _mensajeError;
            set => Asignar(ref _mensajeError, value);
        }

        /// <summary>Lista enlazada con BindableLayout para las tarjetas de prioridad.</summary>
        public ObservableCollection<PrioridadViewModel> Prioridades { get; } = new();

        /// <summary>Tareas ya vencidas y sin completar, ordenadas de más antigua a más reciente.</summary>
        public ObservableCollection<TareaVencidaViewModel> Vencidas { get; } = new();

        public bool TienePrioridades => Prioridades.Count > 0;

        public bool TienePlan => !string.IsNullOrWhiteSpace(Plan);

        public bool TieneVencidas => Vencidas.Count > 0;

        /// <summary>Reinicia la consola para un nuevo análisis.</summary>
        public void Reiniciar()
        {
            EstaCargando = true;
            TieneError = false;
            MensajeError = string.Empty;
            Saludo = "Conectando con el núcleo del asistente...";
            SaludoDetalle = string.Empty;
            Plan = string.Empty;
            Pendientes = 0;
            Prioridades.Clear();
            Vencidas.Clear();
            OnPropertyChanged(nameof(TienePrioridades));
            OnPropertyChanged(nameof(TieneVencidas));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propiedad = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));

        private void Asignar<T>(ref T campo, T valor, [CallerMemberName] string? propiedad = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor)) return;
            campo = valor;
            OnPropertyChanged(propiedad);
        }
    }
}
