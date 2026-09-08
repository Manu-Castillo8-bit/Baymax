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

            // Token JWT del usuario autenticado
            string token = _supabase.Auth.CurrentSession?.AccessToken
                           ?? UserSession.CurrentJwt
                           ?? string.Empty;

            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException(
                    "No hay una sesión válida. Cierra sesión y vuelve a iniciar sesión con internet.");
            }

            // Invocar la Edge Function {projectUrl}/functions/v1/analizar-tareas
            string json = await _supabase.Functions.Invoke(funcion, token);

            if (string.IsNullOrWhiteSpace(json)) return null;

            var resultado = JsonSerializer.Deserialize<ResultadoAgente>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return resultado;
        }
    }
}