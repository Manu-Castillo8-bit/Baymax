using SQLite;

namespace Asistente
{
    /// <summary>
    /// Punto único de acceso a la base de datos local (SQLite), que es la fuente
    /// de verdad de la app: todo se lee de aquí, con o sin conexión a internet.
    ///
    /// Antes cada clase abría su propia conexión al mismo archivo
    /// (asistente.db3). Al centralizarla se evita que dos conexiones compitan
    /// por crear las tablas al mismo tiempo y se reduce el riesgo de bloqueos.
    /// </summary>
    public static class BaseDatos
    {
        private const string NombreDb = "asistente.db3";

        private static readonly object Cerradura = new();
        private static SQLiteAsyncConnection? _conexion;
        private static Task? _inicializando;

        public static string RutaDb => Path.Combine(FileSystem.AppDataDirectory, NombreDb);

        public static SQLiteAsyncConnection Conexion
        {
            get
            {
                if (_conexion != null) return _conexion;

                lock (Cerradura)
                {
                    _conexion ??= new SQLiteAsyncConnection(RutaDb);
                    return _conexion;
                }
            }
        }

        /// <summary>
        /// Devuelve la conexión con todas las tablas locales ya creadas. Es
        /// idempotente y seguro de llamar desde varios hilos a la vez: solo la
        /// primera llamada hace el trabajo, el resto espera a la misma tarea.
        /// </summary>
        public static async Task<SQLiteAsyncConnection> ObtenerAsync()
        {
            if (_inicializando != null)
            {
                await _inicializando;
                return Conexion;
            }

            lock (Cerradura)
            {
                _inicializando ??= CrearEsquemaAsync();
            }

            await _inicializando;
            return Conexion;
        }

        private static async Task CrearEsquemaAsync()
        {
            var db = Conexion;

            // CreateTableAsync es idempotente (CREATE TABLE IF NOT EXISTS), así que
            // se puede llamar en cada arranque sin miedo.
            await db.CreateTableAsync<TareaLocal>();
            await db.CreateTableAsync<UsuarioLocal>();
            await db.CreateTableAsync<RecordatorioEstado>();
        }
    }
}
