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

                // Si el panel es la pantalla de inicio no hay a qué volver: se
                // avisa al servidor y se regresa al login, que es donde se decide
                // qué pantalla corresponde a la cuenta.
                if (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopAsync();
                else
                    App.IrA(new NavigationPage(new LoginPage()));

                return;
            }

            bool hayRed = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            AvisoOffline.IsVisible = !hayRed;

            // Sin conexión el panel queda en pausa con el aviso visible. Se sigue
            // abriendo porque es la pantalla de inicio del administrador, pero sin
            // un error por cada intento de carga.
            if (!hayRed)
            {
                LblSubtitulo.Text = "Sin conexión: los datos se cargarán al recuperar la red.";
                LblVacio.IsVisible = false;
                ListaUsuarios.IsVisible = false;
                return;
            }

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
                Depurador.Registrar("AdminPage.CargarUsuariosAsync", ex);
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
            bool hayRed = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            AvisoOffline.IsVisible = !hayRed;

            if (!hayRed) return;

            await CargarUsuariosAsync();
        }

        private async void OnSalirClicked(object sender, EventArgs e)
        {
            bool confirmado = await DisplayAlertAsync("Cerrar sesión",
                "¿Seguro que deseas cerrar sesión?",
                "Cerrar sesión", "Cancelar");

            if (!confirmado) return;

            // El panel es la pantalla de inicio de esta cuenta, así que aquí no hay
            // una pantalla anterior a la que volver: se detiene el servicio y se
            // vuelve al login, igual que hace MainPage.
            await ServicioFondo.DetenerTodoAsync();
            UserSession.LimpiarSesion();

            App.IrA(new NavigationPage(new LoginPage()));
        }

        private async void OnUsuarioSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not UsuarioAdmin usuario) return;

            ListaUsuarios.SelectedItem = null;

            await Navigation.PushAsync(new AdminDetallePage(usuario));
        }
    }
}
