using Supabase;

namespace Asistente
{
    public partial class RegisterPage : ContentPage
    {
        private Supabase.Client _supabase;

        public RegisterPage()
        {
            InitializeComponent();

            _supabase = new Supabase.Client(Configuracion.SupabaseUrl, Configuracion.SupabaseAnonKey, new SupabaseOptions { AutoRefreshToken = true });
            _ = _supabase.InitializeAsync();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "";

            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                StatusLabel.Text = "Completa todos los campos obligatorios.";
                return;
            }

            try
            {
                string email = EmailEntry.Text.Trim();
                string password = PasswordEntry.Text;
                string nombre = NameEntry.Text.Trim();

                // 1. Crear credenciales en Supabase Auth
                var session = await _supabase.Auth.SignUp(email, password);

                if (session?.User != null)
                {
                    // 2. Insertar el registro de perfil en la tabla usuario
                    var nuevoUsuario = new Usuario
                    {
                        AuthUserId = session.User.Id,
                        Nombre = nombre,
                        Correo = email,
                        Rol = "usuario",
                        FechaRegistro = DateTime.Now
                    };

                    await _supabase.From<Usuario>().Insert(nuevoUsuario);

                    await DisplayAlert("Éxito", "Usuario registrado correctamente. Procede a iniciar sesión.", "OK");
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error al registrar: {ex.Message}";
            }
        }
    }
}