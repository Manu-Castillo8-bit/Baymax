using System.Text.Json;

namespace Asistente
{
    public class AgenteIAServicio
    {
        private readonly Supabase.Client _supabase;

        public AgenteIAServicio(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public class ResultadoAgente
        {
            public string? Nombre { get; set; }
            public int Pendientes { get; set; }
            public string? Plan { get; set; }
            public List<Prioridad>? Prioridades { get; set; }
        }

        public class Prioridad
        {
            public string? Titulo { get; set; }
            public string? Razon { get; set; }
        }

        public async Task<ResultadoAgente?> AnalizarTareasAsync()
        {
            // Endpoint configurado en la Edge Function de Supabase
            string funcion = "analizar-tareas";

            // Token JWT del usuario autenticado (renovándolo si expiró)
            string token = await ObtenerTokenValidoAsync();

            // Invocar la Edge Function {projectUrl}/functions/v1/analizar-tareas
            string json;
            try
            {
                json = await _supabase.Functions.Invoke(funcion, token);
            }
            catch (Exception ex)
            {
                if (EsMensajeDeSesion(ex.Message))
                {
                    throw new InvalidOperationException(
                        "Tu sesión con Supabase expiró o quedó inválida. Cierra sesión y vuelve a iniciar sesión para reactivar el asistente.");
                }
                throw new InvalidOperationException($"El agente no pudo contactar al servidor: {ex.Message}");
            }

            // Si la función respondió con un JSON de error por token inválido/expirado
            if (EsJsonDeErrorDeSesion(json))
            {
                throw new InvalidOperationException(
                    "Tu sesión con Supabase expiró o quedó inválida. Cierra sesión y vuelve a iniciar sesión para reactivar el asistente.");
            }

            if (string.IsNullOrWhiteSpace(json)) return null;

            var resultado = JsonSerializer.Deserialize<ResultadoAgente>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return resultado;
        }

        /// <summary>
        /// Devuelve un access token JWT válido para llamar a la Edge Function.
        /// Si el token guardado expiró (los access tokens de Supabase duran ~1 hora),
        /// se renueva automáticamente con el refresh token almacenado, sin obligar
        /// al usuario a cerrar sesión y volver a iniciar sesión al reabrir la app.
        /// </summary>
        private async Task<string> ObtenerTokenValidoAsync()
        {
            // 1. Sesión ya activa en este cliente (login reciente en esta ejecución)
            var sesionActual = _supabase.Auth.CurrentSession;
            if (sesionActual != null && !string.IsNullOrEmpty(sesionActual.AccessToken))
            {
                return sesionActual.AccessToken;
            }

            // 2. Sin ningún JWT guardado: no hay forma de hablar con la función
            if (string.IsNullOrEmpty(UserSession.CurrentJwt))
            {
                throw new InvalidOperationException(
                    "No hay una sesión válida. Cierra sesión y vuelve a iniciar sesión con internet.");
            }

            // 3. El JWT guardado pudo expirar: renovarlo con el refresh token
            if (!string.IsNullOrEmpty(UserSession.CurrentRefreshToken) &&
                Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    await _supabase.InitializeAsync();
                    var refrescada = await _supabase.Auth.SetSession(
                        UserSession.CurrentJwt, UserSession.CurrentRefreshToken, forceAccessTokenRefresh: true);

                    if (refrescada != null && !string.IsNullOrEmpty(refrescada.AccessToken))
                    {
                        UserSession.CurrentJwt = refrescada.AccessToken;
                        UserSession.CurrentRefreshToken = refrescada.RefreshToken ?? "";
                        UserSession.CurrentAuthId = refrescada.User?.Id ?? UserSession.CurrentAuthId;
                        UserSession.GuardarSesion();
                        return refrescada.AccessToken;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo renovar el JWT guardado: {ex.Message}");
                }
            }

            // 4. Último recurso: devolver el JWT tal cual. Si ya expiró, la Edge
            //    Function lo rechazará y MostrarError mostrará el aviso correcto.
            return UserSession.CurrentJwt;
        }

        /// <summary>
        /// Detecta si un texto suelto (p.ej. el mensaje de una excepción) indica un
        /// problema de sesión con Supabase y no otro tipo de fallo del servidor.
        /// </summary>
        private static bool EsMensajeDeSesion(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            string t = texto.ToLowerInvariant();
            return t.Contains("jwt") || t.Contains("token inválido") || t.Contains("token invalido") ||
                   t.Contains("sesión no autenticada") || t.Contains("sesion no autenticada") ||
                   t.Contains("unauthorized") || t.Contains("refresh token");
        }

        /// <summary>
        /// Detecta si la respuesta JSON de la función es un error de sesión
        /// (p.ej. {"code":"UNAUTORIZED_ASYMETRIC_JWT","message":"invalid JWT"})
        /// y no el resultado de éxito del agente.
        /// </summary>
        private static bool EsJsonDeErrorDeSesion(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            string t = json.ToLowerInvariant();
            bool tieneClaveDeError = t.Contains("\"code\"") || t.Contains("\"error\"");
            return tieneClaveDeError && EsMensajeDeSesion(json);
        }
    }
}