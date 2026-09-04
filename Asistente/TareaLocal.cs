using SQLite;

namespace Asistente
{
    [Table("tarea_local")]
    public class TareaLocal
    {
        [PrimaryKey, AutoIncrement]
        public int IdLocal { get; set; }

        public int? IdTareaServer { get; set; } // ID retornado por Supabase (id_tarea)
        public int IdUsuario { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Estado { get; set; } = "Pendiente";

        // Banderas para control de sincronización
        public bool IsSynced { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime UltimaModificacion { get; set; } = DateTime.Now;
    }
}