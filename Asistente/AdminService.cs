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

        /// <summary>
        /// Refrescar el rol y las llamadas del panel tocan a la vez el cliente de
        /// Supabase desde varios frentes (el temporizador de segundo plano, el
        /// arranque de la ventana y la propia pantalla). InitializeAsync y Rpc no
        /// son seguros ante llamadas simultáneas y se quedaban bloqueando la
        /// interfaz. Con este candado solo corre una a la vez; si otra ya está en
        /// marcha, esta se salta sin esperar, porque dentro de un minuto volverá a
        /// dentro de un minuto volverá a intentarlo.
        /// </summary>
        private static readonly SemaphoreSlim SincronizacionEnCurso = new(1, 1);

        // Ventana de validez de la respuesta del servidor para el rol.
        private static readonly TimeSpan TiempoValidoRol = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan EsperaMaximaRol = TimeSpan.FromSeconds(15);
        private static DateTime _ultimaConsultaRol = DateTime.MinValue;

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

            if (!await SincronizacionEnCurso.WaitAsync(0)) return default;

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
                Depurador.Registrar($"AdminService.RpcAsync('{funcion}')", ex);
                TraducirError(ex);
                throw;
            }
            finally
            {
                SincronizacionEnCurso.Release();
            }
        }

        private static async Task RpcSinResultadoAsync(string funcion, Dictionary<string, object> parametros)
        {
            await VerificarAsync();

            if (!await SincronizacionEnCurso.WaitAsync(0))
                throw new InvalidOperationException("Ya hay una operación del panel en marcha. Inténtalo de nuevo en un momento.");

            try
            {
                var cliente = ServicioFondo.ClienteSupabase;
                await cliente.InitializeAsync();

                var respuesta = await cliente.Rpc(funcion, parametros);
                ComprobarEstado(funcion, respuesta);
            }
            catch (Exception ex)
            {
                Depurador.Registrar($"AdminService.RpcSinResultado('{funcion}')", ex);
                TraducirError(ex);
                throw;
            }
            finally
            {
                SincronizacionEnCurso.Release();
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

            // Se llama desde varios sitios (arranque, temporizador de 60 s, retorno
            // de la conexión, la propia pantalla). Si la respuesta llegó hace poco
            // no se vuelve a preguntar: así se evitan esperas en cadena cuando
            // varias llamadas se solapan.
            if (DateTime.UtcNow - _ultimaConsultaRol < TiempoValidoRol) return;

            // Aquí sí se espera a que termine la otra llamada en vez de saltarla.
            // El rol decide qué pantalla se abre al arrancar, así que descartar
            // esta comprobación dejaría al administrador en la pantalla equivocada
            // hasta el siguiente ciclo.
            if (!await SincronizacionEnCurso.WaitAsync(EsperaMaximaRol)) return;

            try
            {
                var cliente = ServicioFondo.ClienteSupabase;
                await cliente.InitializeAsync();

                // La tabla es la fuente del rol. es_admin() va después como
                // respaldo, nunca antes: si el cliente todavía no tiene sesión
                // activa devuelve false y se acabaría guardando "usuario" encima
                // del admin real, que es justo lo que hace desaparecer el botón.
                string? rol = await ConsultarRolEnTablaAsync(cliente)
                               ?? await ConsultarRolConEsAdminAsync(cliente);

                // La marca se pone solo con una respuesta válida: si la consulta
                // falló, se reintenta en la siguiente llamada en vez de esperar 20 s
                // con un rol que quizá ya no vale.
                if (rol is null) return;

                _ultimaConsultaRol = DateTime.UtcNow;

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
            finally
            {
                SincronizacionEnCurso.Release();
            }
        }

        /// <summary>
        /// Respaldo cuando la tabla no es legible. OJO: es_admin() decide a partir
        /// del JWT, así que devuelve false si la sesión no está activa todavía
        /// (por ejemplo al arrancar con la sesión guardada y el token a punto de
        /// renovarse). Por eso su "usuario" no se toma como cierto si ya teníamos
        /// un rol conocido: se devuelve null y se conserva lo anterior.
        /// </summary>
        private static async Task<string?> ConsultarRolConEsAdminAsync(Supabase.Client cliente)
        {
            try
            {
                // Sin sesión activa el JWT no identifica a nadie y el resultado
                // no significa nada. Se sale sin tocar el rol que ya tenemos.
                if (cliente.Auth.CurrentSession is null) return null;

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
        /// Lee la columna "rol" de la fila de la cuenta activa. Es la fuente que se
        /// usa primero porque no depende del token: al arrancar con la sesión
        /// guardada el JWT puede aún no estar renovándose y es_admin() mentiría.
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
