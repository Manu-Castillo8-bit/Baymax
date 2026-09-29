using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Asistente
{
    // 1. Modelo para la tabla 'usuario' en Supabase
    [Table("usuario")]
    public class Usuario : BaseModel
    {
        [PrimaryKey("id_usuario", false)]
        [Column("id_usuario", ignoreOnInsert: true)]
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
        [Column("id_tarea", ignoreOnInsert: true)]
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
        public string? Estado { get; set; } = EstadoTarea.Pendiente;
    }

    /// <summary>
    /// La misma tabla "tarea" sin la columna de frecuencia. Existe porque
    /// "frecuencia_recordatorio_horas" es una exclusive de Asistente: si la base
    /// compartida se montó sin ella, cualquier INSERT o UPDATE que la incluya
    /// rebota con un 400 y la tarea se quedaría sin subir nunca. Con este modelo
    /// se envía el resto de los campos y la frecuencia queda solo en local.
    /// </summary>
    [Table("tarea")]
    public class TareaSinFrecuencia : BaseModel
    {
        [PrimaryKey("id_tarea", false)]
        [Column("id_tarea", ignoreOnInsert: true)]
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
        public string? Estado { get; set; } = EstadoTarea.Pendiente;
    }

    /// <summary>
    /// Estados de una tarea. La columna "estado" de Supabase es texto libre y
    /// Proyectito escribe valores como "completada" o "completado", así que
    /// aquí se normalizan a los dos valores canónicos que ambas apps usan.
    /// </summary>
    public static class EstadoTarea
    {
        public const string Pendiente = "Pendiente";
        public const string Completado = "Completado";

        public static string Normalizar(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return Pendiente;

            if (estado.StartsWith("complet", StringComparison.OrdinalIgnoreCase))
                return Completado;

            if (estado.StartsWith("pend", StringComparison.OrdinalIgnoreCase))
                return Pendiente;

            return estado.Trim();
        }

        public static bool EsCompletado(string? estado)
            => string.Equals(Normalizar(estado), Completado, StringComparison.OrdinalIgnoreCase);
    }

    // 3. Sesión del usuario activo
    public static class UserSession
    {
        public static int CurrentUserId { get; set; }
        public static string CurrentUserName { get; set; } = string.Empty;
        public static string CurrentAuthId { get; set; } = string.Empty;

        public const string EstadoUsuario = "usuario";
        public const string EstadoAdmin = "admin";

        /// <summary>
        /// Rol de la cuenta ('usuario' o 'admin'). Vive en la sesión para que el
        /// panel de administración pueda mostrarse sin volver a iniciar sesión.
        /// Solo decide qué se ve en la app: el servidor revalida el rol en cada
        /// llamada, así que un rol falso aquí no concede ningún permiso.
        /// </summary>
        public static string CurrentRol { get; set; } = EstadoUsuario;

        public static bool EsAdmin
            => string.Equals(CurrentRol, EstadoAdmin, StringComparison.OrdinalIgnoreCase);

        // Token JWT del usuario (para llamar Edge Functions autenticadas)
        public static string CurrentJwt { get; set; } = string.Empty;

        // Refresh token de Supabase: permite renovar el JWT cuando expira (~1h)
        // sin obligar al usuario a volver a iniciar sesión tras reabrir la app.
        public static string CurrentRefreshToken { get; set; } = string.Empty;

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
            Preferences.Default.Set("Session.Rol", string.IsNullOrWhiteSpace(CurrentRol) ? EstadoUsuario : CurrentRol);
            Preferences.Default.Set("Session.Jwt", CurrentJwt);
            Preferences.Default.Set("Session.RefreshToken", CurrentRefreshToken);
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
            CurrentRol = Preferences.Default.Get("Session.Rol", EstadoUsuario);
            CurrentJwt = Preferences.Default.Get("Session.Jwt", string.Empty);
            CurrentRefreshToken = Preferences.Default.Get("Session.RefreshToken", string.Empty);
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
            CurrentRol = EstadoUsuario;
            CurrentJwt = string.Empty;
            CurrentRefreshToken = string.Empty;
            OfflineEmail = string.Empty;
            OfflinePassword = string.Empty;

            Preferences.Default.Remove("Session.IdUsuario");
            Preferences.Default.Remove("Session.Nombre");
            Preferences.Default.Remove("Session.AuthId");
            Preferences.Default.Remove("Session.Rol");
            Preferences.Default.Remove("Session.Jwt");
            Preferences.Default.Remove("Session.RefreshToken");
            Preferences.Default.Remove("Session.OfflineEmail");
            Preferences.Default.Remove("Session.OfflinePassword");
        }
    }
}