using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Asistente
{
    // 1. Modelo para la tabla 'usuario' en Supabase
    [Table("usuario")]
    public class Usuario : BaseModel
    {
        [PrimaryKey("id_usuario", false)]
        public int IdUsuario { get; set; }

        [Column("auth_user_id")]
        public string? AuthUserId { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("correo")]
        public string Correo { get; set; } = string.Empty;

        [Column("rol")]
        public string? Rol { get; set; } = "usuario";

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }

    // 2. Modelo para la tabla 'tarea' en Supabase (necesario para SyncService)
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

        [Column("frecuencia_recordatorio_horas")]
        public int? FrecuenciaRecordatorioHoras { get; set; }

        [Column("estado")]
        public string? Estado { get; set; } = "Pendiente";
    }

    // 3. Sesión del usuario activo
    public static class UserSession
    {
        public static int CurrentUserId { get; set; }
        public static string CurrentUserName { get; set; } = string.Empty;
        public static string CurrentAuthId { get; set; } = string.Empty;

        // Token JWT del usuario (para llamar Edge Functions autenticadas)
        public static string CurrentJwt { get; set; } = string.Empty;
    }
}