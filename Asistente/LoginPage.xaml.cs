using Supabase;

namespace Asistente
{
    public partial class LoginPage : ContentPage
    {
        private Supabase.Client _supabase;

        public LoginPage()
        {
            InitializeComponent();

            string url = "https://mmvzkwklwibugzpyawmy.supabase.co";
            string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1tdnprd2tsd2lidWd6cHlhd215Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODgxODE0NTgsImV4cCI6MjEwMzc1NzQ1OH0.41uyn16H27UbRaV1aKBGGPonFw_48Q1rdsHV2TKcQp8";

            _supabase = new Supabase.Client(url, key, new SupabaseOptions { AutoRefreshToken = true });
            _ = _supabase.InitializeAsync();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "";
            string email = EmailEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusLabel.Text = "Por favor completa todos los campos.";
                return;
            }

            try
            {
                // 1. Iniciar sesión con el servicio Auth de Supabase
                var session = await _supabase.Auth.SignIn(email, password);

                if (session?.User != null)
                {
                    string authId = session.User.Id;

                    // 2. Buscar el registro del perfil en la tabla 'usuario' usando el UUID
                    var response = await _supabase.From<Usuario>()
                        .Where(u => u.AuthUserId == authId)
                        .Get();

                    var user = response.Models.FirstOrDefault();

                    if (user != null)
                    {
                        // Guardar los datos del usuario en la sesión global
                        UserSession.CurrentUserId = user.IdUsuario;
                        UserSession.CurrentUserName = user.Nombre;
                        UserSession.CurrentAuthId = authId;

                        Application.Current.MainPage = new MainPage();
                    }
                    else
                    {
                        StatusLabel.Text = "Usuario autenticado, pero no tiene perfil en la tabla 'usuario'.";
                    }
                }
                else
                {
                    StatusLabel.Text = "Credenciales inválidas. Acceso denegado.";
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error de inicio de sesión: {ex.Message}";
            }
        }

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}