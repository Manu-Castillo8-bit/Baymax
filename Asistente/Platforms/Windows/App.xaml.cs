using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Asistente.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // La bandeja del sistema se encarga de que la "X" solo oculte la
            // ventana en vez de cerrar el proceso. Asi el servicio de fondo sigue
            // vivo y los recordatorios continue saliendo con la ventana cerrada.
            try
            {
                var ventanaMaui = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
                if (ventanaMaui?.Handler?.PlatformView is Microsoft.UI.Xaml.Window ventana)
                    BandejaSistema.Configurar(ventana);
            }
            catch { }
        }
    }

}
