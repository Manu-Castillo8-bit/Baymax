namespace Asistente
{
    /// <summary>
    /// Muestra las notificaciones como un mensaje emergente dentro de la app,
    /// además del aviso (y el sonido) que pone el sistema operativo.
    ///
    /// Es la misma doble capa que usa la app de referencia: mientras el usuario
    /// tiene la ventana a la vista, cada notificación sale también como cuadro
    /// con su texto; si la app está oculta en la bandeja, minimizada o tapada
    /// por otra ventana, el aviso se queda en el toast del sistema, que ya
    /// suena y aparece solo. Hacer saltar un cuadro por detrás de otras
    /// ventanas sería molesto y quedaría sin leer.
    ///
    /// Los cuadros se enseñan de uno en uno: si llegan dos avisos seguidos, el
    /// segundo espera a que el usuario cierre el primero, para no apilar
    /// diálogos unos encima de otros.
    /// </summary>
    public static class AvisosPantalla
    {
        private static readonly SemaphoreSlim Cola = new(1, 1);
        private static readonly object Cerradura = new();

        // El mismo aviso puede llegar por dos caminos (el notificador del
        // sistema y el evento del plugin de notificaciones), así que se
        // recuerda el último mostrado para no repetir el cuadro en cuestión
        // de segundos.
        private static string? _ultimaClave;
        private static DateTime _ultimoAviso = DateTime.MinValue;

        private static readonly TimeSpan MargenDuplicado = TimeSpan.FromSeconds(8);

        /// <summary>
        /// Pide que se muestre el mensaje emergente. No bloquea: el cuadro se
        /// enseña en su propio hilo de trabajo y, si la app pasa a segundo
        /// plano antes de salir, se descarta sin más.
        /// </summary>
        public static void Mostrar(string titulo, string mensaje)
        {
            if (!ServicioFondo.EnPrimerPlano) return;

            string clave = $"{titulo}\n{mensaje}";
            lock (Cerradura)
            {
                if (clave == _ultimaClave && DateTime.Now - _ultimoAviso < MargenDuplicado)
                {
                    return;
                }

                _ultimaClave = clave;
                _ultimoAviso = DateTime.Now;
            }

            _ = MostrarAsync(titulo, mensaje);
        }

        private static async Task MostrarAsync(string titulo, string mensaje)
        {
            try
            {
                // Un aviso a la vez: si mientras espera su turno el usuario ya
                // está cerrando otro, este se descarta en cuanto toque comprobar
                // el primer plano.
                await Cola.WaitAsync();

                try
                {
                    if (!ServicioFondo.EnPrimerPlano) return;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var pagina = Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (pagina is null) return;

                        await pagina.DisplayAlertAsync(titulo, mensaje, "OK");
                    });
                }
                finally
                {
                    Cola.Release();
                }
            }
            catch (Exception ex)
            {
                // Un cuadro que no se pudo abrir no debe romper el aviso del
                // sistema: el toast ya ha llegado igual.
                System.Diagnostics.Debug.WriteLine($"AvisosPantalla: {ex.Message}");
            }
        }
    }
}
