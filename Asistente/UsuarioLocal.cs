using SQLite;

namespace Asistente
{
    // ... tus clases Usuario y Tarea de Supabase se mantienen igual ...

    // Modelo para guardar usuarios en la BD SQLite local
    [Table("usuario_local")]
    public class UsuarioLocal
    {
        [PrimaryKey]
        public int IdUsuario { get; set; }
        public string AuthUserId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Rol { get; set; }
    }
}