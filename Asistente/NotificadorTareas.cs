using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace Asistente
{
    /// <summary>
    /// Utilidades compartidas para programar notificaciones de recordatorio de tareas.
    /// </summary>
    public static class NotificadorTareas
    {
        public static void ProgramarRecordatorio(int idTarea, string titulo, DateTime? fechaVencimiento, int frecuenciaHoras)
        {
            try
            {
                var request = new NotificationRequest
                {
                    NotificationId = idTarea, // ID local de la tarea para evitar duplicados y poder cancelarla
                    Title = "⏰ Recordatorio: Tarea Próxima",
                    Description = $"La tarea: '{titulo}' está pendiente.",
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule()
                };

                if (frecuenciaHoras > 0)
                {
                    // Modo recurrente: recordar cada X horas
                    request.Schedule.NotifyTime = DateTimeOffset.Now.AddSeconds(5);
                    request.Schedule.RepeatType = NotificationRepeat.TimeInterval;
                    request.Schedule.NotifyRepeatInterval = TimeSpan.FromHours(frecuenciaHoras);
                }
                else if (fechaVencimiento.HasValue)
                {
                    // Modo simple: avisar 1 día antes del vencimiento (sin repetición)
                    DateTime fechaNotificacion = fechaVencimiento.Value.AddDays(-1);
                    if (fechaNotificacion <= DateTime.Now) return;

                    request.Schedule.NotifyTime = new DateTimeOffset(fechaNotificacion);
                    request.Schedule.RepeatType = NotificationRepeat.No;
                }
                else
                {
                    return; // sin fecha y sin frecuencia: no hay nada que programar
                }

                LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al programar notificación: {ex.Message}");
            }
        }

        public static void CancelarRecordatorio(int idTarea)
        {
            try
            {
                LocalNotificationCenter.Current.Cancel(idTarea);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cancelar notificación: {ex.Message}");
            }
        }
    }
}