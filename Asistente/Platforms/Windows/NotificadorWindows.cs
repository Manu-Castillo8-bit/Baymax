using System.Globalization;
using System.Text;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Asistente
{
    /// <summary>
    /// Programador de notificaciones a nivel de sistema operativo para Windows.
    /// Usa ScheduledToastNotification de Windows (que el SO dispara aunque la app
    /// esté completamente cerrada) en lugar de los temporizadores en memoria del
    /// plugin de notificaciones, que se pierden al cerrar la aplicación.
    ///
    /// El instalador (AsistenteSetup.iss) y este código registran el mismo AUMID
    /// y el mismo CLSID de activación, para que los recordatorios programados
    /// sobrevivan al cierre total del proceso.
    /// </summary>
    public static class NotificadorWindows
    {
        public const string Aumid = "AsistenteApp";
        public const string ClsidActivador = "{B1E3A4D7-5F2C-4A8B-9D1E-6C7F3A2B5E81}";

        private static ToastNotifier? _notifier;
        private static readonly object Cerradura = new();
        private static bool _registroAsegurado;

        /// <summary>
        /// Programa un recordatorio puntual a nivel de SO para una hora futura.
        /// Devuelve true solo si el aviso quedó realmente en la cola del sistema;
        /// false si no se agendó (para que el temporizador de ServicioFondo lo
        /// muestre él, en vez de creerse que el sistema ya lo entregó).
        /// </summary>
        public static bool Programar(int id, string titulo, DateTimeOffset cuando)
        {
            try
            {
                var retraso = cuando - DateTimeOffset.Now;
                if (retraso <= TimeSpan.Zero) return false; // ya venció: no hay nada que agendar
                if (retraso < TimeSpan.FromSeconds(10)) return false; // demasiado cerca: lo muestra el temporizador

                if (!ObtenerNotifier(out var notifier) || notifier is null) return false;

                // AddToSchedule solo AÑADE: si este mismo aviso ya estuviera en la
                // cola, se acumularían copias y el usuario recibiría el recordatorio
                // varias veces. Se retira antes cualquier copia previa del mismo id,
                // dejando como mucho un aviso en cola por tarea.
                Cancelar(id);

                var xml = ConstruirXml(id, "⏰ Recordatorio: Tarea Próxima", $"La tarea: '{titulo}' está pendiente.");
                if (xml is null) return false;

                var programada = new ScheduledToastNotification(xml, cuando);
                notifier.AddToSchedule(programada);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.Programar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Muestra una notificación inmediata (toast) a nivel de sistema operativo.
        /// Funciona 100% sin conexión a internet porque el toast es local.
        /// Devuelve true si se mostró y false si falla.
        /// </summary>
        public static bool Mostrar(int id, string titulo, string mensaje)
        {
            try
            {
                if (!ObtenerNotifier(out var notifier) || notifier is null) return false;

                var xml = ConstruirXml(id, titulo, mensaje);
                if (xml is null) return false;

                notifier.Show(new ToastNotification(xml));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.Mostrar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cancela el recordatorio programado a nivel de SO para el id indicado.
        /// </summary>
        public static void Cancelar(int id)
        {
            try
            {
                if (!ObtenerNotifier(out var notifier) || notifier is null) return;

                var objetivo = $"launch=\"notificationId={id.ToString(CultureInfo.InvariantCulture)}\"";
                var programadas = notifier.GetScheduledToastNotifications().ToList();

                foreach (var programada in programadas)
                {
                    string contenido;
                    try
                    {
                        contenido = programada.Content.GetXml();
                    }
                    catch
                    {
                        continue;
                    }

                    if (contenido.Contains(objetivo, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            notifier.RemoveFromSchedule(programada);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"NotificadorWindows.Cancelar (remove): {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.Cancelar: {ex.Message}");
            }
        }

        /// <summary>
        /// Devuelve la hora para la que el sistema tiene en cola un aviso de este
        /// id, o null si no hay ninguno.
        ///
        /// Es la clave para no duplicar avisos: lo que se agenda desaparece de la
        /// cola en cuanto el sistema lo muestra, así que la ausencia del aviso
        /// significa que ya se notificó (y no que quedara pendiente).
        /// </summary>
        public static DateTimeOffset? ObtenerProgramada(int id)
        {
            try
            {
                if (!ObtenerNotifier(out var notifier) || notifier is null) return null;

                var objetivo = $"notificationId={id.ToString(CultureInfo.InvariantCulture)}";
                var programadas = notifier.GetScheduledToastNotifications().ToList();

                foreach (var programada in programadas)
                {
                    try
                    {
                        if (programada.Content.GetXml().Contains(objetivo, StringComparison.OrdinalIgnoreCase))
                        {
                            return programada.DeliveryTime;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.ObtenerProgramada: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cancela todo lo programado a nivel de SO para esta aplicación.
        /// </summary>
        public static void CancelarTodo()
        {
            try
            {
                if (!ObtenerNotifier(out var notifier) || notifier is null) return;

                var programadas = notifier.GetScheduledToastNotifications().ToList();
                foreach (var programada in programadas)
                {
                    try
                    {
                        notifier.RemoveFromSchedule(programada);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"NotificadorWindows.CancelarTodo (remove): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.CancelarTodo: {ex.Message}");
            }
        }

        private static bool ObtenerNotifier(out ToastNotifier? notifier)
        {
            notifier = null;
            try
            {
                if (!OperatingSystem.IsWindows()) return false;

                lock (Cerradura)
                {
                    if (_notifier != null)
                    {
                        notifier = _notifier;
                        return true;
                    }

                    AsegurarRegistro();
                    _notifier = ToastNotificationManager.CreateToastNotifier(Aumid);
                    notifier = _notifier;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.ObtenerNotifier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registra el AUMID y el CLSID de activación en el registro de Windows
        /// (HKCU) para apps no empaquetadas. El instalador hace lo mismo.
        /// Así Windows sabe mostrar y activar los toasts programados incluso
        /// con la app cerrada del todo.
        ///
        /// Las rutas se refrescan siempre para que apunten al ejecutable que se
        /// está ejecutando de verdad. Si la app se movió de sitio (por ejemplo al
        /// instalar una versión nueva), dejar la ruta anterior haría que Windows
        /// no pudiera mostrar los avisos y que al pulsar uno se abriera el
        /// ejecutable viejo.
        /// </summary>
        private static void AsegurarRegistro()
        {
            if (_registroAsegurado) return;

            try
            {
                string exePath = Environment.ProcessPath ?? string.Empty;

                using (var aumidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{Aumid}", true))
                {
                    if (aumidKey != null)
                    {
                        if (aumidKey.GetValue("DisplayName") == null)
                        {
                            aumidKey.SetValue("DisplayName", "Asistente");
                        }

                        if (!string.IsNullOrWhiteSpace(exePath))
                        {
                            aumidKey.SetValue("IconUri", $"{exePath},0");
                        }

                        aumidKey.SetValue("CustomActivator", ClsidActivador);
                    }
                }

                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    using var clsidKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{ClsidActivador}\LocalServer32", true);
                    clsidKey?.SetValue(string.Empty, exePath);
                }

                _registroAsegurado = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.AsegurarRegistro: {ex.Message}");
            }
        }

        private static XmlDocument? ConstruirXml(int id, string textoPrincipal, string textoDetalle)
        {
            try
            {
                string argumento = $"notificationId={id.ToString(CultureInfo.InvariantCulture)}";
                string cuerpo =
                    $"<toast launch=\"{argumento}\">" +
                    "<visual><binding template=\"ToastGeneric\">" +
                    $"<text>{EscapeXml(textoPrincipal)}</text>" +
                    $"<text>{EscapeXml(textoDetalle)}</text>" +
                    "</binding></visual></toast>";

                var xml = new XmlDocument();
                xml.LoadXml(cuerpo);
                return xml;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificadorWindows.ConstruirXml: {ex.Message}");
                return null;
            }
        }

        private static string EscapeXml(string valor)
        {
            var builder = new StringBuilder(valor.Length);
            foreach (var caracter in valor)
            {
                switch (caracter)
                {
                    case '&': builder.Append("&amp;"); break;
                    case '<': builder.Append("&lt;"); break;
                    case '>': builder.Append("&gt;"); break;
                    case '"': builder.Append("&quot;"); break;
                    case '\'': builder.Append("&apos;"); break;
                    default: builder.Append(caracter); break;
                }
            }
            return builder.ToString();
        }
    }
}