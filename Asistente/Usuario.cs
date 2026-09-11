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

        // Credenciales temporales (solo en memoria) para re-autenticación
        // con Supabase al recuperar conexión (nunca se guardan en disco)
        public static string OfflineEmail { get; set; } = string.Empty;
        public static string OfflinePassword { get; set; } = string.Empty;

        // Guarda la sesión activa en Preferences para que al reabrir la app
        // el usuario entre directo a sus tareas sin volver a iniciar sesión.
        public static void GuardarSesion()
        {
            Preferences.Default.Set("Session.IdUsuario", CurrentUserId);
            Preferences.Default.Set("Session.Nombre", CurrentUserName);
            Preferences.Default.Set("Session.AuthId", CurrentAuthId);
            Preferences.Default.Set("Session.Jwt", CurrentJwt);
            Preferences.Default.Set("Session.OfflineEmail", OfflineEmail);
            Preferences.Default.Set("Session.OfflinePassword", OfflinePassword);
        }

        // Restaura la sesión guardada. Devuelve true si hay una sesión válida.
        public static bool CargarSesionGuardada()
        {
            if (!Preferences.Default.ContainsKey("Session.IdUsuario")) return false;

            int id = Preferences.Default.Get("Session.IdUsuario", 0);
            if (id == 0) return false;

            CurrentUserId = id;
            CurrentUserName = Preferences.Default.Get("Session.Nombre", string.Empty);
            CurrentAuthId = Preferences.Default.Get("Session.AuthId", string.Empty);
            CurrentJwt = Preferences.Default.Get("Session.Jwt", string.Empty);
            OfflineEmail = Preferences.Default.Get("Session.OfflineEmail", string.Empty);
            OfflinePassword = Preferences.Default.Get("Session.OfflinePassword", string.Empty);
            return true;
        }

        // Limpia la sesión activa y la guardada (se usa al cerrar sesión).
        public static void LimpiarSesion()
        {
            CurrentUserId = 0;
            CurrentUserName = string.Empty;
            CurrentAuthId = string.Empty;
            CurrentJwt = string.Empty;
            OfflineEmail = string.Empty;
            OfflinePassword = string.Empty;

            Preferences.Default.Remove("Session.IdUsuario");
            Preferences.Default.Remove("Session.Nombre");
            Preferences.Default.Remove("Session.AuthId");
            Preferences.Default.Remove("Session.Jwt");
            Preferences.Default.Remove("Session.OfflineEmail");
            Preferences.Default.Remove("Session.OfflinePassword");
        }
    }
}