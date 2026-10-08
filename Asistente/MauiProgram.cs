using Plugin.LocalNotification; // <-- Agregar este namespace
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace Asistente
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // <-- Habilitar Notificaciones
                .UseLocalNotification(config =>
                {
                    // Canal de Android de alta importancia: es el que permite
                    // que el aviso salga como mensaje emergente (banner) con
                    // sonido. Si no se declara, el plugin crea uno por defecto
                    // con prioridad baja y el aviso pasa casi desapercibido.
                    config.AddAndroid(android =>
                    {
                        android.AddChannel(new AndroidNotificationChannelRequest
                        {
                            Id = NotificadorTareas.CanalRecordatorios,
                            Name = "Recordatorios",
                            Description = "Avisos de tareas pendientes",
                            Importance = AndroidImportance.High
                        });
                    });
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var app = builder.Build();

            // Además del aviso del sistema, mientras la app está a la vista se
            // muestra el mensaje emergente en pantalla. Se suscribe aquí (y no en
            // una página) para que funcione también cuando la app arranca desde
            // cero por una notificación programada.
            try
            {
                LocalNotificationCenter.Current.NotificationReceived += e =>
                {
                    var solicitud = e.Request;
                    if (solicitud is null) return;

                    AvisosPantalla.Mostrar(
                        string.IsNullOrWhiteSpace(solicitud.Title) ? "Notificación" : solicitud.Title,
                        solicitud.Description ?? string.Empty);
                };
            }
            catch
            {
                // Si la plataforma no soporta el evento, el aviso del sistema
                // sigue llegando por su cuenta.
            }

            return app;
        }
    }
}
