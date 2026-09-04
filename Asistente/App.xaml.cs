using Microsoft.Extensions.DependencyInjection;

namespace Asistente
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new LoginPage());
        }

       
    }
}