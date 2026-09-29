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
            // Si hay una sesión guardada, entrar directo a las tareas
            Page raiz = UserSession.CargarSesionGuardada()
                ? new NavigationPage(new MainPage())
                : new NavigationPage(new LoginPage());

            var window = new Window(raiz);

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
