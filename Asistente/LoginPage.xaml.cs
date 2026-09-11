using SQLite;
using Supabase;
using System.Security.Cryptography;
using System.Text;

namespace Asistente
{
    public partial class LoginPage : ContentPage
    {
        private Supabase.Client _supabase;

        public LoginPage()
        {
            InitializeComponent();

            _supabase = new Supabase.Client(Configuracion.SupabaseUrl, Configuracion.SupabaseAnonKey, new SupabaseOptions { AutoRefreshToken = true });
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "";

            // Animación de carga en el botón
            MostrarCarga(true);

            try
            {
                await EjecutarLoginAsync();
            }
            finally
            {
                MostrarCarga(false);
            }
        }

        private void MostrarCarga(bool cargando)
        {
            LoginActivityIndicator.IsVisible = cargando;
            LoginActivityIndicator.IsRunning = cargando;
            LoginButton.IsEnabled = !cargando;
            LoginButton.Text = cargando ? "CARGANDO..." : "INICIAR SESIÓN";
        }

        private async Task EjecutarLoginAsync()
        {
            string email = EmailEntry.Text?.Trim().ToLower() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusLabel.Text = "Por favor completa todos los campos.";
                return;
            }

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "asistente.db3");
            var dbLocal = new SQLiteAsyncConnection(dbPath);
            await dbLocal.CreateTableAsync<UsuarioLocal>();

            // 1. MODO ONLINE: Hay conexión a Internet
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    await _supabase.InitializeAsync();
                    var session = await _supabase.Auth.SignIn(email, password);

                    if (session?.User != null)
                    {
                        string authId = session.User.Id;

                        var response = await _supabase.From<Usuario>()
                            .Where(u => u.AuthUserId == authId)
                            .Get();

                        var user = response.Models.FirstOrDefault();

                        if (user != null)
                        {
                            // A. Asignar sesión activa
                            UserSession.CurrentUserId = user.IdUsuario;
                            UserSession.CurrentUserName = user.Nombre;
                            UserSession.CurrentAuthId = authId;
                            UserSession.CurrentJwt = session.AccessToken ?? "";
                            UserSession.OfflineEmail = string.Empty;
                            UserSession.OfflinePassword = string.Empty;

                            // B. Respaldar en la BD local SQLite para permitir inicios de sesión Offline
                            var usuarioLocal = new UsuarioLocal
                            {
                                IdUsuario = user.IdUsuario,
                                AuthUserId = authId,
                                Nombre = user.Nombre,
                                Correo = email,
                                PasswordHash = GenerarSHA256(password),
                                Rol = user.Rol
                            };

                            await dbLocal.InsertOrReplaceAsync(usuarioLocal);

                            // C. Guardar la sesión para entrar directo la próxima vez
                            UserSession.GuardarSesion();

                            Application.Current.MainPage = new MainPage();
                        }
                        else
                        {
                            StatusLabel.Text = "Usuario sin perfil en la base de datos.";
                        }
                    }
                    else
                    {
                        StatusLabel.Text = "Credenciales incorrectas.";
                    }
                }
                catch (Exception ex)
                {
                    StatusLabel.Text = $"Error de servidor: {ex.Message}";
                }
            }
            // 2. MODO OFFLINE: Sin conexión a Internet
            else
            {
                try
                {
                    // Buscar si las credenciales existen guardadas localmente
                    var usuarioLocal = await dbLocal.Table<UsuarioLocal>()
                        .FirstOrDefaultAsync(u => u.Correo == email);

                    if (usuarioLocal != null)
                    {
                        string inputHash = GenerarSHA256(password);

                        if (usuarioLocal.PasswordHash == inputHash)
                        {
                            UserSession.CurrentUserId = usuarioLocal.IdUsuario;
                            UserSession.CurrentUserName = usuarioLocal.Nombre;
                            UserSession.CurrentAuthId = usuarioLocal.AuthUserId;
                            UserSession.CurrentJwt = string.Empty;
                            // Guardar credenciales (solo en memoria) para re-autenticarse
                            // con Supabase al recuperar conexión
                            UserSession.OfflineEmail = usuarioLocal.Correo;
                            UserSession.OfflinePassword = password;

                            // Guardar la sesión para entrar directo la próxima vez
                            UserSession.GuardarSesion();

                            Application.Current.MainPage = new MainPage();
                        }
                        else
                        {
                            StatusLabel.Text = "Contraseña incorrecta (Validación local).";
                        }
                    }
                    else
                    {
                        StatusLabel.Text = "Debes iniciar sesión con internet al menos una vez en este dispositivo.";
                    }
                }
                catch (Exception ex)
                {
                    StatusLabel.Text = $"Error de inicio de sesión local: {ex.Message}";
                }
            }
        }

        private void OnShowPasswordToggled(object sender, CheckedChangedEventArgs e)
        {
            PasswordEntry.IsPassword = !e.Value;
        }

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }

        // Método auxiliar para encriptar la contraseña localmente con SHA-256
        private string GenerarSHA256(string texto)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}