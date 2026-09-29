using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Responses;

namespace Asistente
{
    // ── DTOs de lectura del panel de administración ────────────────────────────
    // Se leen con RPC de Supabase, no con las tablas directamente: las funciones
    // son SECURITY DEFINER y comprueban con es_admin() que quien llama es
    // administrador, así que el permiso no depende de ocultar botones en la app.

    public class UsuarioAdmin
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
        [JsonPropertyName("correo")] public string Correo { get; set; } = "";
        [JsonPropertyName("rol")] public string Rol { get; set; } = "";

        public bool EsAdmin => string.Equals(Rol, UserSession.EstadoAdmin, StringComparison.OrdinalIgnoreCase);

        public string Iniciales
        {
            get
            {
                var n = (Nombre ?? "").Trim();
                if (n.Length == 0) return "?";
                var partes = n.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string iniciales = partes.Length >= 2
                    ? partes[0][..1] + partes[^1][..1]
                    : n[..1];
                return iniciales.ToUpperInvariant();
            }
        }

        public Color BadgeColor => EsAdmin ? Color.FromArgb("#B8860B") : Color.FromArgb("#1E3A5F");

        public string DescripcionRol => EsAdmin ? "Administrador" : "Usuario";
    }

    public class AdminTarea
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
        [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
        [JsonPropertyName("fecha_vencimiento")] public DateTime? FechaVencimiento { get; set; }
        [JsonPropertyName("estado")] public string Estado { get; set; } = "";

        public bool EstaCompletada => EstadoTarea.EsCompletado(Estado);

        public string FechaTexto => FechaVencimiento.HasValue
            ? FechaVencimiento.Value.ToLocalTime().ToString("dd/MM/yyyy")
            : "sin fecha";

        public Color EstadoColor => EstaCompletada
            ? Color.FromArgb("#6EE7B7")
            : Color.FromArgb("#FBBF24");
    }

    // El servidor devuelve también las otras tablas del usuario (movimientos,
    // contraseñas y recordatorios de salud). Se mapean para no perder nada al
    // deserializar, aunque este panel solo gestione tareas.

    public class AdminMovimiento
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("monto")] public decimal Monto { get; set; }
        [JsonPropertyName("tipo")] public string Tipo { get; set; } = "";
        [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
        [JsonPropertyName("fecha")] public DateTime Fecha { get; set; }
    }

    public class AdminContrasena
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("sitio")] public string Sitio { get; set; } = "";
        [JsonPropertyName("usuario")] public string UsuarioCuenta { get; set; } = "";
    }

    public class AdminRecordatorio
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("tipo")] public string Tipo { get; set; } = "";
        [JsonPropertyName("frecuencia_minutos")] public int FrecuenciaMinutos { get; set; }
        [JsonPropertyName("activo")] public bool Activo { get; set; }
    }

    public class AdminDatosUsuario
    {
        [JsonPropertyName("usuario")] public UsuarioAdmin? Usuario { get; set; }
        [JsonPropertyName("tareas")] public List<AdminTarea> Tareas { get; set; } = new();
        [JsonPropertyName("movimientos")] public List<AdminMovimiento> Movimientos { get; set; } = new();
        [JsonPropertyName("contrasenas")] public List<AdminContrasena> Contrasenas { get; set; } = new();
        [JsonPropertyName("recordatorios")] public List<AdminRecordatorio> Recordatorios { get; set; } = new();
    }

    // ── Servicio del panel ─────────────────────────────────────────────────────

    /// <summary>
    /// Acceso a los datos de administración mediante las funciones RPC de
    /// Supabase. Solo funciona con conexión: el panel es una herramienta de
    /// gestión, no algo que deba quedar disponible sin internet.
    /// </summary>
    public static class AdminService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static bool EsAdmin => UserSession.EsAdmin;

        private static async Task VerificarAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                throw new InvalidOperationException("Necesitas conexión a internet para usar el panel de administrador.");

            if (!EsAdmin)
                throw new InvalidOperationException("No tienes permisos de administrador.");

            // Con sesión offline no hay JWT, y sin JWT el servidor no reconoce a
            // quien llama. Aprovechar la conexión para activarlo.
            await ServicioFondo.AsegurarSesionSupabaseAsync();
        }

        private static Dictionary<string, object> Parametros(params (string nombre, object valor)[] pares)
            => pares.ToDictionary(p => p.nombre, p => p.valor);

        /// <summary>
        /// Ejecuta un RPC que devuelve datos y los convierte al tipo pedido.
        /// Traduce el error típico de autorización a un mensaje entendible.
        /// </summary>
        private static async Task<T?> RpcAsync<T>(string funcion, Dictionary<string, object>? parametros = null)
            where T : class
        {
            await VerificarAsync();

            try
            {
                var cliente = ServicioFondo.ClienteSupabase;
                await cliente.InitializeAsync();

                var respuesta = await cliente.Rpc(funcion, parametros ?? new Dictionary<string, object>());
                ComprobarEstado(funcion, respuesta);
                var contenido = await LeerContenidoAsync(respuesta);

                return Deserializar<T>(contenido);
            }
            catch (Exception ex)
            {
                TraducirError(ex);
                throw;
            }
        }

        private static async Task RpcSinResultadoAsync(string funcion, Dictionary<string, object> parametros)
        {
            await VerificarAsync();

            try
            {
                var cliente = ServicioFondo.ClienteSupabase;
                await cliente.InitializeAsync();

                var respuesta = await cliente.Rpc(funcion, parametros);
                ComprobarEstado(funcion, respuesta);
            }
            catch (Exception ex)
            {
                TraducirError(ex);
                throw;
            }
        }

        /// <summary>
        /// Las funciones de escritura devuelven "void", así que no hay cuerpo que
        /// revisar: si el servidor las rechazó (permisos, parámetro inválido), el
        /// fallo solo aparece en el estado HTTP y sería silencioso sin esto.
        /// </summary>
        private static void ComprobarEstado(string funcion, BaseResponse respuesta)
        {
            var http = respuesta.ResponseMessage;
            if (http is null || http.IsSuccessStatusCode) return;

            throw new InvalidOperationException(
                $"El servidor rechazó '{funcion}' (HTTP {(int)http.StatusCode}). Revisa que tu cuenta siga siendo administradora.");
        }

        private static async Task<string> LeerContenidoAsync(BaseResponse respuesta)
        {
            if (!string.IsNullOrWhiteSpace(respuesta.Content))
                return respuesta.Content;

            if (respuesta.ResponseMessage?.Content is { } contenido)
                return await contenido.ReadAsStringAsync();

            return "";
        }

        private static void TraducirError(Exception ex)
        {
            if (ex.Message.Contains("No autorizado", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("P0001", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "El servidor rechazó la sesión de administrador. " +
                    "Verifica en Supabase que tu correo tenga rol 'admin' y que el enlace con tu cuenta de Auth esté correcto. " +
                    "Después cierra sesión y vuelve a iniciar sesión en la app.", ex);
            }
        }

        private static T? Deserializar<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return default;

            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        // ── Consultas ──────────────────────────────────────────────────────────

        public static async Task<List<UsuarioAdmin>> ListarUsuariosAsync()
        {
            var resultado = await RpcAsync<List<UsuarioAdmin>>("admin_listar_usuarios");
            return resultado ?? new List<UsuarioAdmin>();
        }

        public static async Task<AdminDatosUsuario> ObtenerDatosUsuarioAsync(int idUsuario)
        {
            var resultado = await RpcAsync<AdminDatosUsuario>(
                "admin_datos_usuario", Parametros(("p_usuario", idUsuario)));

            return resultado ?? new AdminDatosUsuario();
        }

        // ── Cuentas ────────────────────────────────────────────────────────────

        /// <summary>
        /// Cambia el nombre y/o el rol de una cuenta. Los parámetros que llegan a
        /// null se dejan como están en el servidor.
        /// </summary>
        public static Task ActualizarUsuarioAsync(int idUsuario, string? nombre, string? rol)
        {
            var parametros = new Dictionary<string, object> { ["p_usuario"] = idUsuario };
            if (!string.IsNullOrWhiteSpace(nombre)) parametros["p_nombre"] = nombre;
            if (!string.IsNullOrWhiteSpace(rol)) parametros["p_rol"] = rol;
            return RpcSinResultadoAsync("admin_actualizar_usuario", parametros);
        }

        public static Task EliminarUsuarioAsync(int idUsuario)
            => RpcSinResultadoAsync("admin_eliminar_usuario", Parametros(("p_usuario", idUsuario)));

        // ── Tareas de cualquier cuenta ─────────────────────────────────────────

        public static Task InsertarTareaAsync(int idUsuario, string titulo, string descripcion, DateTime? fecha, string estado)
            => RpcSinResultadoAsync("admin_insertar_tarea", new Dictionary<string, object>
            {
                ["p_usuario"] = idUsuario,
                ["p_titulo"] = titulo,
                ["p_descripcion"] = descripcion ?? "",
                ["p_fecha_vencimiento"] = (object?)fecha ?? DBNull.Value,
                ["p_estado"] = EstadoTarea.Normalizar(estado)
            });

        public static Task ActualizarTareaAsync(int idUsuario, int id, string? titulo, string? descripcion, DateTime? fecha, string? estado)
        {
            var parametros = new Dictionary<string, object>
            {
                ["p_usuario"] = idUsuario,
                ["p_tarea"] = (long)id
            };
            if (!string.IsNullOrWhiteSpace(titulo)) parametros["p_titulo"] = titulo;
            if (descripcion is not null) parametros["p_descripcion"] = descripcion;
            if (fecha is not null) parametros["p_fecha_vencimiento"] = fecha.Value;
            if (!string.IsNullOrWhiteSpace(estado)) parametros["p_estado"] = EstadoTarea.Normalizar(estado);
            return RpcSinResultadoAsync("admin_actualizar_tarea", parametros);
        }

        public static Task EliminarTareaAsync(int idUsuario, int id)
            => RpcSinResultadoAsync("admin_eliminar_tarea", new Dictionary<string, object>
            {
                ["p_usuario"] = idUsuario,
                ["p_id"] = (long)id
            });

        // ── Rol de la cuenta activa ────────────────────────────────────────────

        /// <summary>
        /// Vuelve a leer el rol de la cuenta activa desde el servidor. Si alguien
        /// le concedió (o le quitó) el rol de administrador, la app se entera sin
        /// necesidad de cerrar sesión.
        /// </summary>
        public static async Task RefrescarRolAsync()
        {
            if (UserSession.CurrentUserId == 0) return;
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            try
            {
                var cliente = ServicioFondo.ClienteSupabase;
                await cliente.InitializeAsync();

                string? rol = await ConsultarRolConEsAdminAsync(cliente)
                               ?? await ConsultarRolEnTablaAsync(cliente);

                if (rol is null) return;

                if (string.Equals(rol, UserSession.CurrentRol, StringComparison.OrdinalIgnoreCase)) return;

                UserSession.CurrentRol = rol;
                UserSession.GuardarSesion();

                // La copia local del usuario queda al día para el próximo login offline.
                try
                {
                    var db = await BaseDatos.ObtenerAsync();
                    var local = await db.Table<UsuarioLocal>()
                        .FirstOrDefaultAsync(u => u.IdUsuario == UserSession.CurrentUserId);

                    if (local is not null)
                    {
                        local.Rol = rol;
                        await db.UpdateAsync(local);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AdminService: no se pudo actualizar el rol local: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // Sin conexión efectiva o error del servidor: se conserva el rol actual.
                System.Diagnostics.Debug.WriteLine($"AdminService: no se pudo refrescar el rol: {ex.Message}");
            }
        }

        /// <summary>
        /// es_admin() es la misma comprobación que usan las funciones del panel, así
        /// que no depende de que la tabla "usuario" sea legible desde la app (por
        /// ejemplo, si alguien activa RLS sobre ella más adelante).
        /// </summary>
        private static async Task<string?> ConsultarRolConEsAdminAsync(Supabase.Client cliente)
        {
            try
            {
                var respuesta = await cliente.Rpc("es_admin", new Dictionary<string, object>());
                var contenido = await LeerContenidoAsync(respuesta);

                if (bool.TryParse(contenido, out bool esAdmin))
                    return esAdmin ? UserSession.EstadoAdmin : UserSession.EstadoUsuario;

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminService: es_admin() no respondió: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Respaldo: si es_admin() no está disponible en el proyecto, se lee la
        /// fila de la cuenta activa.
        /// </summary>
        private static async Task<string?> ConsultarRolEnTablaAsync(Supabase.Client cliente)
        {
            var respuesta = await cliente.From<Usuario>()
                .Where(u => u.IdUsuario == UserSession.CurrentUserId)
                .Get();

            var remoto = respuesta.Models.FirstOrDefault();
            if (remoto is null) return null;

            return string.IsNullOrWhiteSpace(remoto.Rol) ? UserSession.EstadoUsuario : remoto.Rol;
        }
    }
}
