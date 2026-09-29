namespace Asistente
{
    /// <summary>
    /// Lista de cuentas de la base compartida con Proyectito. Desde aquí se entra
    /// al detalle de cada usuario para gestionar sus tareas.
    ///
    /// Esta pantalla solo muestra el botón si la sesión es de administrador, pero
    /// la comprobación real está en el servidor: aunque alguien entre a mano, las
    /// funciones RPC rechazan la llamada.
    /// </summary>
    public partial class AdminPage : ContentPage
    {
        private List<UsuarioAdmin> _todos = new();

        public AdminPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!AdminService.EsAdmin)
            {
                await DisplayAlertAsync("Aviso", "No tienes permisos de administrador.", "OK");
                await Navigation.PopAsync();
                return;
            }

            AvisoOffline.IsVisible = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
            await CargarUsuariosAsync();
        }

        private async Task CargarUsuariosAsync()
        {
            MostrarIndicador(true);

            try
            {
                _todos = await AdminService.ListarUsuariosAsync();

                int admins = _todos.Count(u => u.EsAdmin);
                LblSubtitulo.Text = $"{_todos.Count} cuenta(s) · {admins} administrador(es) · {_todos.Count - admins} usuario(s)";

                AplicarFiltro();
            }
            catch (Exception ex)
            {
                LblSubtitulo.Text = "No se pudieron cargar las cuentas.";
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                MostrarIndicador(false);
            }
        }

        private void AplicarFiltro()
        {
            string texto = TxtBuscar.Text?.Trim().ToLower() ?? "";

            var lista = string.IsNullOrEmpty(texto)
                ? _todos
                : _todos.Where(u =>
                    (u.Nombre ?? "").ToLower().Contains(texto) ||
                    (u.Correo ?? "").ToLower().Contains(texto)).ToList();

            ListaUsuarios.ItemsSource = lista;
            ListaUsuarios.IsVisible = lista.Count > 0;
            LblVacio.IsVisible = lista.Count == 0;
        }

        private void MostrarIndicador(bool visible)
        {
            Indicador.IsRunning = visible;
            Indicador.IsVisible = visible;
        }

        private void OnBuscarChanged(object sender, TextChangedEventArgs e) => AplicarFiltro();

        private async void OnRefrescarClicked(object sender, EventArgs e)
        {
            AvisoOffline.IsVisible = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
            await CargarUsuariosAsync();
        }

        private async void OnVolverClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        private async void OnUsuarioSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not UsuarioAdmin usuario) return;

            ListaUsuarios.SelectedItem = null;

            await Navigation.PushAsync(new AdminDetallePage(usuario));
        }
    }
}
