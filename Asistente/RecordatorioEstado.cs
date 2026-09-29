using SQLite;

namespace Asistente
{
    /// <summary>
    /// Estado del ciclo de recordatorios de una tarea.
    ///
    /// Existe porque conviven dos mecanismos de aviso que pueden disparar la
    /// misma ocurrencia:
    ///   1. El temporizador en segundo plano (mientras el proceso está vivo).
    ///   2. La notificación programada en el sistema operativo (si el proceso muere).
    ///
    /// Guardando la ocurrencia "pendiente" en disco, ambos mecanismos saben
    /// que se trata del mismo aviso y no lo muestran dos veces. Al comprobar
    /// que el toast programado por el SO ya no está en la cola, se deduce que
    /// el sistema ya lo mostró y se avanza a la siguiente ocurrencia.
    /// </summary>
    [Table("recordatorio_estado")]
    public class RecordatorioEstado
    {
        [PrimaryKey, AutoIncrement]
        public int IdLocal { get; set; }

        /// <summary>Id local de la tarea (TareaLocal.IdLocal).</summary>
        [Indexed]
        public int IdTarea { get; set; }

        /// <summary>Momento en que debe saltar el próximo aviso.</summary>
        public DateTime ProximaOcurrencia { get; set; }

        /// <summary>
        /// True si el sistema operativo tiene un toast en cola para
        /// <see cref="ProximaOcurrencia"/>. Si está en cola y ya pasó la hora,
        /// significa que el SO ya lo mostró y no hay que repetirlo.
        /// </summary>
        public bool ProgramadoEnSistema { get; set; }

        /// <summary>Último aviso que se mostró de esta tarea (para diagnóstico).</summary>
        public DateTime UltimoAviso { get; set; }
    }
}
