using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using SQLite;

namespace Asistente
{
    /// <summary>
    /// Utilidades compartidas para programar notificaciones de recordatorio de tareas.
    /// </summary>
    public static class NotificadorTareas
    {
        /// <summary>
        /// Reprograma en el sistema operativo todas las notificaciones de las tareas
        /// pendientes guardadas en la BD local. Debe ejecutarse al abrir la app para
        /// restaurar recordatorios que el SO pudo haber limpiado al cerrar la app.
        /// Funciona sin conexión (las notificaciones son locales, no requieren internet).
        /// </summary>
        public static async Task ReprogramarRecordatoriosPendientesAsync()
        {
            try
            {
                if (UserSession.CurrentUserId == 0) return;

#if WINDOWS
                // En Windows los toasts se agregan al programador del SO (no se
                // reemplazan), así que limpiamos antes de volver a registrarlos.
                if (OperatingSystem.IsWindows())
                {
                    NotificadorWindows.CancelarTodo();
                }
#endif

                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "asistente.db3");
                var dbLocal = new SQLiteAsyncConnection(dbPath);
                await dbLocal.CreateTableAsync<TareaLocal>();

                var tareas = await dbLocal.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == UserSession.CurrentUserId && !t.IsDeleted && t.Estado != "Completado")
                    .ToListAsync();

                foreach (var tarea in tareas)
                {
                    if ((tarea.FrecuenciaRecordatorioHoras ?? 0) > 0)
                    {
                        // Recordatorio cíclico: restaurar la próxima ocurrencia alineada
                        // con la cadencia establecida (sin disparar una avalancha al abrir).
                        var ancla = tarea.UltimaModificacion > DateTime.Now.AddHours(-(tarea.FrecuenciaRecordatorioHoras ?? 0))
                            ? tarea.UltimaModificacion
                            : DateTime.Now;
                        ProgramarRecordatorioCiclico(tarea.IdLocal, tarea.Titulo, tarea.FrecuenciaRecordatorioHoras ?? 0, ancla);
                    }
                    else if (tarea.FechaVencimiento.HasValue)
                    {
                        // Recordatorio puntual: 1 día antes del vencimiento
                        ProgramarRecordatorio(tarea.IdLocal, tarea.Titulo, tarea.FechaVencimiento, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al reprogramar recordatorios: {ex.Message}");
            }
        }

        /// <summary>
        /// Programa un recordatorio cíclico respetando la cadencia original:
        /// calcula la siguiente ocurrencia futura alineada al intervalo elegido,
        /// partiendo de la última modificación de la tarea.
        /// </summary>
        private static void ProgramarRecordatorioCiclico(int idTarea, string titulo, int frecuenciaHoras, DateTime ancla)
        {
            try
            {
                DateTime proximo = ancla.AddHours(frecuenciaHoras);
                // Avanzar hasta encontrar una ocurrencia futura
                while (proximo <= DateTime.Now)
                {
                    proximo = proximo.AddHours(frecuenciaHoras);
                }

#if WINDOWS
                if (OperatingSystem.IsWindows())
                {
                    NotificadorWindows.Cancelar(idTarea);
                    NotificadorWindows.Programar(idTarea, titulo, new DateTimeOffset(proximo));
                    return;
                }
#endif

                var request = new NotificationRequest
                {
                    NotificationId = idTarea,
                    Title = "⏰ Recordatorio: Tarea Próxima",
                    Description = $"La tarea: '{titulo}' está pendiente.",
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = new DateTimeOffset(proximo),
                        RepeatType = NotificationRepeat.TimeInterval,
                        NotifyRepeatInterval = TimeSpan.FromHours(frecuenciaHoras)
                    }
                };

                LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al programar recordatorio cíclico: {ex.Message}");
            }
        }

        public static void ProgramarRecordatorio(int idTarea, string titulo, DateTime? fechaVencimiento, int frecuenciaHoras)
        {
#if WINDOWS
            // En Windows la programación se delega al SO (ScheduledToastNotification)
            // para que los recordatorios se disparen aunque la app esté cerrada del todo.
            if (OperatingSystem.IsWindows())
            {
                NotificadorWindows.Cancelar(idTarea);
                if (frecuenciaHoras > 0)
                {
                    NotificadorWindows.Programar(idTarea, titulo, DateTimeOffset.Now.AddHours(frecuenciaHoras));
                }
                else if (fechaVencimiento.HasValue)
                {
                    DateTime fechaNotificacion = fechaVencimiento.Value.AddDays(-1);
                    if (fechaNotificacion <= DateTime.Now) return;
                    NotificadorWindows.Programar(idTarea, titulo, new DateTimeOffset(fechaNotificacion));
                }
                return;
            }
#endif
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

        /// <summary>
        /// Muestra una notificación inmediata de confirmación (por ejemplo al crear
        /// o editar una tarea). Funciona sin conexión: en Windows usa el toast nativo
        /// del SO y en el resto de plataformas el centro de notificaciones local.
        /// </summary>
        public static void MostrarNotificacionInmediata(string titulo, string mensaje)
        {
            try
            {
#if WINDOWS
                if (OperatingSystem.IsWindows())
                {
                    if (NotificadorWindows.Mostrar(new Random().Next(1000, 9999), titulo, mensaje))
                    {
                        return;
                    }
                }
#endif
                var request = new NotificationRequest
                {
                    NotificationId = new Random().Next(1000, 9999),
                    Title = titulo,
                    Description = mensaje,
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTimeOffset.Now.AddSeconds(2)
                    }
                };

                LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al mostrar notificación inmediata: {ex.Message}");
            }
        }

        public static void CancelarRecordatorio(int idTarea)
        {
#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                NotificadorWindows.Cancelar(idTarea);
            }
#endif
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