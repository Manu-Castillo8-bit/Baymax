namespace Asistente
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Crea la ventana principal y arranca el servicio en segundo plano.
        ///
        /// El servicio se arranca aqui (y no en una pagina) a proposito: asi sigue
        /// vivo cuando el usuario cierra la ventana, que en Windows la "X" lo
        /// unico que hace es ocultarla en la bandeja del sistema. De esa forma los
        /// recordatorios se siguen mostrando y la sincronizacion con Supabase
        /// continua aunque la app no este a la vista.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
        // Si hay una sesión guardada, entrar directo. Un administrador va
            // directo al panel; el resto, a la lista de tareas.
            bool haySesion = UserSession.CargarSesionGuardada();

            Page raiz = haySesion
                ? CrearPantallaDeInicio()
                : new NavigationPage(new LoginPage());

            var window = new Window(raiz);

            // La sesión guardada puede traer un rol viejo: por ejemplo, si se le
            // concedió el rol de administrador con la app cerrada. Se espera a que
            // la ventana exista de verdad (Created) porque antes de eso todavía no
            // está registrada en Application.Windows y no hay a qué moverse.
            if (haySesion)
            {
                window.Created += async (_, _) => await CorregirPantallaSiEsAdminAsync();
            }

            window.Activated += (_, _) =>
            {
                ServicioFondo.EnPrimerPlano = true;

                // Cada vez que el usuario vuelve a la app se comprueba si hay red
                // para ponerse al dia con el servidor.
                _ = ServicioFondo.SincronizarEnSegundoPlanoAsync();
            };

            window.Deactivated += (_, _) => ServicioFondo.EnPrimerPlano = false;
            window.Stopped += (_, _) => ServicioFondo.EnPrimerPlano = false;

            ServicioFondo.Arrancar();

            return window;
        }

        /// <summary>
        /// Reconfirma el rol con el servidor y mueve la ventana al panel si la
        /// cuenta resultó ser administradora. Se ejecuta al arrancar, cuando la
        /// sesión guardada aún puede tener el rol de antes.
        /// </summary>
        private static async Task CorregirPantallaSiEsAdminAsync()
        {
            try
            {
                await AdminService.RefrescarRolAsync();

                var ventana = Current?.Windows.FirstOrDefault();
                if (ventana?.Page is not NavigationPage nav) return;

                // Ya está en el panel: no hay nada que corregir.
                if (UserSession.EsAdmin && nav.CurrentPage is AdminPage) return;

                if (UserSession.EsAdmin && nav.CurrentPage is MainPage)
                {
                    MainThread.BeginInvokeOnMainThread(() => nav.PushAsync(new AdminPage()));
                    return;
                }

                // El rol se le quitó: si estaba en el panel, vuelve a sus tareas.
                // Se cambia la página de la ventana en vez de manipular la pila,
                // porque NavigationPage expone NavigationStack como IList y desde
                // fuera no hay forma de vaciarlo sin replace.
                if (!UserSession.EsAdmin && nav.CurrentPage is AdminPage)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                        ventana.Page = new NavigationPage(new MainPage()));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App: no se pudo ajustar la pantalla inicial: {ex.Message}");
            }
        }

        /// <summary>
        /// Pantalla a la que entra una sesión ya iniciada. La cuenta de
        /// administrador abre directamente en el panel y no pasa por la lista de
        /// tareas, porque su trabajo está en el panel. Cualquier otra cuenta entra
        /// a sus tareas de siempre.
        ///
        /// Centralizarlo aquí evita que el login, el arranque y el registro
        /// decidedan cada uno por su cuenta y se desincronicen.
        /// </summary>
        public static NavigationPage CrearPantallaDeInicio()
            => new(UserSession.EsAdmin ? new AdminPage() : (Page)new MainPage());

        /// <summary>
        /// Cambia la pantalla actual. Reemplaza al uso de Application.MainPage, que
        /// esta obsoleto y ademas no funciona bien cuando la ventana esta oculta
        /// en la bandeja del sistema.
        /// </summary>
        public static void IrA(Page pagina)
        {
            var ventana = Current?.Windows.FirstOrDefault();
            if (ventana is null)
            {
                // Solo puede pasar si se navega antes de que exista la ventana, lo
                // que en esta app no ocurre: CreateWindow la crea siempre al
                // arrancar. Se avisa por log en vez de usar MainPage (obsoleto).
                System.Diagnostics.Debug.WriteLine("App.IrA: no hay ventana todavía, se ignora la navegación.");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => ventana.Page = pagina);
        }
    }
}
