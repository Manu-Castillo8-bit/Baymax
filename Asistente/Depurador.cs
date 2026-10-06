namespace Asistente;

public static class Depurador
{
    private static readonly object Cerradura = new();

    /// <summary>
    /// Registra una excepción en un archivo temporal para diagnosticar fallos
    /// del panel de administración sin depurador a mano.
    /// </summary>
    public static void Registrar(string contexto, Exception ex)
    {
        try
        {
            var archivo = Path.Combine(Path.GetTempPath(), "asistente_admin.log");
            string línea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexto}: {ex}";
            lock (Cerradura)
            {
                File.AppendAllText(archivo, línea + Environment.NewLine);
            }
        }
        catch
        {
            // La depuración jamás debe tumbar la app.
        }
    }
}