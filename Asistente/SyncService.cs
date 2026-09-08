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
                // 0. Sincronizar borrados: eliminar en Supabase y limpiar localmente
                var borradas = await _dbLocal.Table<TareaLocal>()
                    .Where(t => t.IsDeleted && t.IdUsuario == UserSession.CurrentUserId)
                    .ToListAsync();

                foreach (var borrada in borradas)
                {
                    if (borrada.IdTareaServer.HasValue)
                    {
                        // La tarea ya existía en el servidor: eliminarla de Supabase
                        await _supabase.From<Tarea>()
                            .Where(x => x.IdTarea == borrada.IdTareaServer.Value)
                            .Delete();
                    }
                    // Eliminar la fila local (marcada como borrada)
                    await _dbLocal.DeleteAsync(borrada);
                }

                // 1. Subir cambios locales no sincronizados a Supabase
                var noSincronizadas = await _dbLocal.Table<TareaLocal>()
                    .Where(t => !t.IsSynced && !t.IsDeleted && t.IdUsuario == UserSession.CurrentUserId)
                    .ToListAsync();

                foreach (var local in noSincronizadas)
                {
                    var tareaServer = new Tarea
                    {
                        IdTarea = local.IdTareaServer ?? 0,
                        IdUsuario = local.IdUsuario,
                        Titulo = local.Titulo,
                        Descripcion = local.Descripcion,
                        FechaVencimiento = local.FechaVencimiento,
                        FrecuenciaRecordatorioHoras = local.FrecuenciaRecordatorioHoras,
                        Estado = local.Estado
                    };

                    if (local.IdTareaServer.HasValue)
                    {
                        // Ya existía en el servidor: actualizarla (no crear duplicado)
                        await _supabase.From<Tarea>()
                            .Where(x => x.IdTarea == local.IdTareaServer.Value)
                            .Set(x => x.Titulo, local.Titulo)
                            .Set(x => x.Descripcion, local.Descripcion)
                            .Set(x => x.FechaVencimiento, local.FechaVencimiento)
                            .Set(x => x.FrecuenciaRecordatorioHoras, local.FrecuenciaRecordatorioHoras)
                            .Set(x => x.Estado, local.Estado)
                            .Update();
                    }
                    else
                    {
                        // Tarea nueva: insertarla en el servidor
                        var res = await _supabase.From<Tarea>().Insert(tareaServer);
                        var insertada = res.Models.FirstOrDefault();
                        if (insertada != null)
                        {
                            local.IdTareaServer = insertada.IdTarea;
                        }
                    }

                    local.IsSynced = true;
                    await _dbLocal.UpdateAsync(local);
                }

                // 2. Descargar tareas más recientes desde Supabase y actualizar SQLite
                var respuestaServer = await _supabase.From<Tarea>()
                    .Where(t => t.IdUsuario == UserSession.CurrentUserId)
                    .Get();

                var idsDelServidor = respuestaServer.Models.Select(m => m.IdTarea).ToHashSet();

                // 2a. Tareas locales sincronizadas que el servidor ya no tiene => fueron borradas
                //     en otro dispositivo: borrarlas también localmente.
                var locales = await _dbLocal.Table<TareaLocal>()
                    .Where(t => t.IdUsuario == UserSession.CurrentUserId && !t.IsDeleted)
                    .ToListAsync();

                foreach (var local in locales)
                {
                    if (local.IdTareaServer.HasValue && !idsDelServidor.Contains(local.IdTareaServer.Value))
                    {
                        await _dbLocal.DeleteAsync(local);
                    }
                }

                // 2b. Insertar las del servidor que no existen localmente
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
                            FrecuenciaRecordatorioHoras = server.FrecuenciaRecordatorioHoras,
                            Estado = server.Estado ?? "Pendiente",
                            IsSynced = true,
                            UltimaModificacion = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Mostrar el error de forma visible (no tragárselo en silencio)
                string mensaje = $"Error al sincronizar con el servidor: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(mensaje);
                try
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (page != null)
                        {
                            await page.DisplayAlertAsync("Error de sincronización",
                                "No se pudo sincronizar las tareas con el servidor.\n\nDetalle: " + ex.Message,
                                "OK");
                        }
                    });
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"No se pudo mostrar el aviso de error: {ex2.Message}");
                }
            }
        }
    }
}