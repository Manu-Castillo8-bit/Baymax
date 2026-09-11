using Microsoft.Extensions.DependencyInjection;

namespace Asistente
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Si hay una sesión guardada, entrar directo a las tareas
            if (UserSession.CargarSesionGuardada())
                MainPage = new NavigationPage(new MainPage());
            else
                MainPage = new NavigationPage(new LoginPage());
        }

       
    }
}