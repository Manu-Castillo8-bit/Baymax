using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using SQLite;

namespace Asistente
{
    /// <summary>
    /// Utilidades para mostrar y cancelar los avisos de las tareas.
    ///
    /// Todas las notificaciones son locales: no pasan por internet y por tanto
    /// se muestran igual sin conexión.
    ///
    /// En Windows hay dos capas que cooperate para que el aviso llegue siempre:
    ///   - <see cref="ElSistemaRepiteElAviso"/> es false, así que el aviso lo
    ///     dispara <see cref="ServicioFondo"/> (mientras el proceso está vivo,
    ///     aunque la ventana esté oculta en la bandeja) y además se deja una copia
    ///     agendada en el sistema operativo por si el proceso muere.
    ///   - En Android, iOS y Mac el sistema se encarga de repetir el aviso aunque
    ///     la app esté cerrada del todo, así que ahí no hay nada que hacer.
    /// </summary>
    public static class NotificadorTareas
    {
        /// <summary>
        /// Los ids de tarea (TareaLocal.IdLocal) son números cortos y correlativos.
        /// Los avisos que no pertenecen a una tarea (confirmaciones, pruebas) usan
        /// esta base, para que un aviso puntual nunca pueda pisar ni cancelar el
        /// recordatorio de una tarea.
        /// </summary>
        private const int BaseIdAvisoPuntual = 1_000_000;
        private static int _contadorAvisoPuntual;

        /// <summary>
        /// True cuando el sistema operativo ya repite el aviso por sí solo
        /// (Android, iOS, MacCatalyst). En Windows es false porque allí el
        /// Windows App SDK no agenda notificaciones repetidas.
        /// </summary>
        public static bool ElSistemaRepiteElAviso =>
#if WINDOWS
            false;
#else
            true;
#endif

        /// <summary>
        /// Muestra el recordatorio de una tarea. Lo llama
        /// <see cref="ServicioFondo"/> cuando la ocurrencia toca.
        /// </summary>
        public static void MostrarRecordatorio(TareaLocal tarea)
        {
            try
            {
                // En Windows se usa el toast nativo del SO: funciona con la app
                // oculta en la bandeja y también si el proceso está cerrado.
#if WINDOWS
                if (OperatingSystem.IsWindows() &&
                    NotificadorWindows.Mostrar(tarea.IdLocal, "⏰ Recordatorio: Tarea Próxima",
                        $"La tarea: '{tarea.Titulo}' está pendiente."))
                {
                    return;
                }
#endif
                MostrarConPlugin(tarea.IdLocal, "⏰ Recordatorio: Tarea Próxima",
                    $"La tarea: '{tarea.Titulo}' está pendiente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al mostrar recordatorio: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra un aviso puntual (por ejemplo al crear o editar una tarea).
        /// No colisiona con los ids de recordatorio de las tareas.
        /// </summary>
        public static void MostrarNotificacionInmediata(string titulo, string mensaje)
        {
            try
            {
                int id = BaseIdAvisoPuntual + Interlocked.Increment(ref _contadorAvisoPuntual);

#if WINDOWS
                if (OperatingSystem.IsWindows() && NotificadorWindows.Mostrar(id, titulo, mensaje))
                {
                    return;
                }
#endif
                MostrarConPlugin(id, titulo, mensaje);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al mostrar notificación inmediata: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra el aviso a través del centro de notificaciones local, que es lo
        /// que se usa en móvil y como respaldo en Windows.
        /// </summary>
        private static void MostrarConPlugin(int id, string titulo, string mensaje)
        {
            LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = id,
                Title = titulo,
                Description = mensaje,
                BadgeNumber = 1,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTimeOffset.Now.AddSeconds(2)
                }
            });
        }

        /// <summary>
        /// Deja el recordatorio de una tarea agendado. En Windows el aviso lo
        /// lleva <see cref="ServicioFondo"/> (que además guarda una copia en el
        /// sistema operativo), así que aquí solo se cancela lo anterior para que
        /// el cambio surja de inmediato. En el resto de plataformas se agenda en
        /// el sistema con repetición, que sobrevive al cierre de la app.
        /// </summary>
        public static void ProgramarRecordatorio(int idTarea, string titulo, DateTime? fechaVencimiento, int frecuenciaHoras)
        {
            CancelarRecordatorio(idTarea);

#if WINDOWS
            // El temporizador de ServicioFondo se encarga de agendar y mostrar.
            if (OperatingSystem.IsWindows())
            {
                return;
            }
#endif
            try
            {
                var request = new NotificationRequest
                {
                    NotificationId = idTarea,
                    Title = "⏰ Recordatorio: Tarea Próxima",
                    Description = $"La tarea: '{titulo}' está pendiente.",
                    BadgeNumber = 1,
                    Schedule = new NotificationRequestSchedule()
                };

                if (frecuenciaHoras > 0)
                {
                    // Modo recurrente: recordar cada X horas.
                    request.Schedule.NotifyTime = DateTimeOffset.Now.AddSeconds(5);
                    request.Schedule.RepeatType = NotificationRepeat.TimeInterval;
                    request.Schedule.NotifyRepeatInterval = TimeSpan.FromHours(frecuenciaHoras);
                }
                else if (fechaVencimiento.HasValue)
                {
                    // Modo simple: avisar 1 día antes del vencimiento (sin repetición).
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
        /// Deja una copia del próximo aviso de una tarea agendada en el sistema
        /// operativo, para que salte aunque el proceso esté cerrado. Devuelve
        /// true si el aviso quedó de verdad en la cola.
        ///
        /// Solo tiene efecto en Windows: en el resto de plataformas el sistema
        /// ya se ocupa de repetir el aviso sin necesitar que el proceso siga vivo.
        /// </summary>
        public static bool ProgramarCopiaEnSistema(int idTarea, string titulo, DateTimeOffset cuando)
        {
#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                return NotificadorWindows.Programar(idTarea, titulo, cuando);
            }
#endif
            _ = idTarea;
            _ = titulo;
            _ = cuando;
            return false;
        }

        /// <summary>
        /// Indica si el sistema operativo tiene en cola el aviso de esta tarea.
        /// Si estaba agendado y ya no está, es que el sistema ya lo mostró.
        /// </summary>
        public static bool CopiaEnSistemaExiste(int idTarea)
        {
#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                return NotificadorWindows.ObtenerProgramada(idTarea).HasValue;
            }
#endif
            _ = idTarea;
            return false;
        }

        /// <summary>
        /// Cancela el recordatorio de una tarea, tanto en el sistema operativo
        /// como en el centro de notificaciones local.
        /// </summary>
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
