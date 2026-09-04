using SQLite;
using Supabase;

namespace Asistente
{
    public class SyncService
    {
        private readonly SQLiteAsyncConnection _dbLocal;
        private readonly Supabase.Client _supabase;

        public SyncService(string dbPath, Supabase.Client supabase)
        {
            _dbLocal = new SQLiteAsyncConnection(dbPath);
            _dbLocal.CreateTableAsync<TareaLocal>().Wait();
            _supabase = supabase;

            // Escuchar cambios de conectividad
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                await SincronizarTareasAsync();
            }
        }

        public async Task SincronizarTareasAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            try
            {
                // 1. Subir cambios locales no sincronizados a Supabase
                var noSincronizadas = await _dbLocal.Table<TareaLocal>()
                    .Where(t => !t.IsSynced && t.IdUsuario == UserSession.CurrentUserId)
                    .ToListAsync();

                foreach (var local in noSincronizadas)
                {
                    var tareaServer = new Tarea
                    {
                        IdUsuario = local.IdUsuario,
                        Titulo = local.Titulo,
                        Descripcion = local.Descripcion,
                        FechaVencimiento = local.FechaVencimiento,
                        Estado = local.Estado
                    };

                    var res = await _supabase.From<Tarea>().Insert(tareaServer);
                    var insertada = res.Models.FirstOrDefault();

                    if (insertada != null)
                    {
                        local.IdTareaServer = insertada.IdTarea;
                        local.IsSynced = true;
                        await _dbLocal.UpdateAsync(local);
                    }
                }

                // 2. Descargar tareas más recientes desde Supabase y actualizar SQLite
                var respuestaServer = await _supabase.From<Tarea>()
                    .Where(t => t.IdUsuario == UserSession.CurrentUserId)
                    .Get();

                foreach (var server in respuestaServer.Models)
                {
                    var existeLocal = await _dbLocal.Table<TareaLocal>()
                        .FirstOrDefaultAsync(t => t.IdTareaServer == server.IdTarea);

                    if (existeLocal == null)
                    {
                        await _dbLocal.InsertAsync(new TareaLocal
                        {
                            IdTareaServer = server.IdTarea,
                            IdUsuario = server.IdUsuario,
                            Titulo = server.Titulo,
                            Descripcion = server.Descripcion,
                            FechaVencimiento = server.FechaVencimiento,
                            Estado = server.Estado ?? "Pendiente",
                            IsSynced = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de sync: {ex.Message}");
            }
        }
    }
}