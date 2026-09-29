using System.Globalization;

namespace Asistente
{
    /// <summary>
    /// Detalle de una cuenta: sus datos, su rol y la lista de tareas.
    ///
    /// Todo lo que se cambia aquí escribe directamente en la tabla compartida con
    /// Proyectito, así que el usuario ve los cambios en su app la próxima vez que
    /// sincronice. Si esta cuenta es la del propio administrador, su copia local
    /// se refresca al instante al volver a la pantalla principal.
    /// </summary>
    public partial class AdminDetallePage : ContentPage
    {
        private readonly UsuarioAdmin _usuario;
        private AdminDatosUsuario? _datos;

        public AdminDetallePage(UsuarioAdmin usuario)
        {
            InitializeComponent();
            _usuario = usuario;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            LblNombreDet.Text = _usuario.Nombre;
            LblCorreoDet.Text = _usuario.Correo;
            LblInicialesDet.Text = _usuario.Iniciales;
            AplicarRolEnUI();

            await RecargarAsync();
        }

        private void AplicarRolEnUI()
        {
            LblRolDet.Text = _usuario.DescripcionRol;
            BordeRol.BackgroundColor = _usuario.BadgeColor;
        }

        private async Task RecargarAsync()
        {
            MostrarIndicador(true);

            try
            {
                _datos = await AdminService.ObtenerDatosUsuarioAsync(_usuario.Id);

                // El servidor es la fuente de la verdad: si alguien cambió el
                // nombre o el rol por otra vía, se refleja aquí.
                if (_datos.Usuario is { } remoto)
                {
                    _usuario.Nombre = remoto.Nombre;
                    _usuario.Correo = remoto.Correo;
                    _usuario.Rol = remoto.Rol;

                    LblNombreDet.Text = _usuario.Nombre;
                    LblCorreoDet.Text = _usuario.Correo;
                    LblInicialesDet.Text = _usuario.Iniciales;
                    AplicarRolEnUI();
                }

                Renderizar();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                MostrarIndicador(false);
            }
        }

        private void MostrarIndicador(bool visible)
        {
            Indicador.IsRunning = visible;
            Indicador.IsVisible = visible;
        }

        // ── Render ─────────────────────────────────────────────────────────────

        private void Renderizar()
        {
            var stack = Contenido;
            stack.Children.Clear();

            if (_datos is null) return;

            if (_datos.Tareas.Count == 0)
            {
                stack.Children.Add(Vacio("Esta cuenta todavía no tiene tareas."));
                return;
            }

            int pendientes = _datos.Tareas.Count(t => !t.EstaCompletada);
            stack.Children.Add(Vacio(
                $"{_datos.Tareas.Count} tarea(s) · {pendientes} pendiente(s) · " +
                $"{_datos.Tareas.Count - pendientes} completada(s)"));

            foreach (var tarea in _datos.Tareas.OrderBy(t => t.EstaCompletada).ThenBy(t => t.FechaVencimiento))
                stack.Children.Add(CrearFilaTarea(tarea));
        }

        private static Label Vacio(string texto) => new()
        {
            Text = texto,
            TextColor = Color.FromArgb("#64748B"),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 6)
        };

        private View CrearFilaTarea(AdminTarea tarea)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#111827"),
                BorderColor = tarea.EstaCompletada ? Color.FromArgb("#065F46") : Color.FromArgb("#245B78"),
                CornerRadius = 12,
                Padding = new Thickness(14),
                HasShadow = false
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto)
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                RowSpacing = 4
            };

            var titulo = new Label
            {
                Text = tarea.Titulo,
                TextColor = Color.FromArgb("#E2E8F0"),
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var detalle = new Label
            {
                Text = $"{tarea.Estado} · {tarea.FechaTexto}",
                TextColor = tarea.EstadoColor,
                FontSize = 12,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            grid.Add(titulo, 0, 0);
            grid.Add(detalle, 0, 1);

            if (!string.IsNullOrWhiteSpace(tarea.Descripcion))
            {
                var descripcion = new Label
                {
                    Text = tarea.Descripcion,
                    TextColor = Color.FromArgb("#64748B"),
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 2
                };
                grid.Add(descripcion, 0, 2);
                RowDefinition filaDescripcion = new() { Height = GridLength.Auto };
                grid.RowDefinitions.Add(filaDescripcion);
            }

            var botones = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            botones.Children.Add(CrearBotonIcono("✎", Color.FromArgb("#1E3A5F"), Color.FromArgb("#38BDF8"),
                async () => await EditarTareaAsync(tarea)));
            botones.Children.Add(CrearBotonIcono(
                tarea.EstaCompletada ? "↺" : "✔",
                Color.FromArgb("#052E16"),
                Color.FromArgb("#6EE7B7"),
                () => CambiarEstadoAsync(tarea)));
            botones.Children.Add(CrearBotonIcono("🗑", Color.FromArgb("#450A0A"), Color.FromArgb("#F87171"),
                () => EliminarTareaAsync(tarea)));

            grid.Add(botones, 1, 0);
            Grid.SetRowSpan(botones, 3);

            frame.Content = grid;
            return frame;
        }

        private static Button CrearBotonIcono(string texto, Color fondo, Color color, Func<Task> accion)
        {
            var boton = new Button
            {
                Text = texto,
                BackgroundColor = fondo,
                TextColor = color,
                FontSize = 13,
                CornerRadius = 8,
                HeightRequest = 34,
                WidthRequest = 42,
                Padding = new Thickness(0),
                Margin = new Thickness(0)
            };
            boton.Clicked += async (_, _) => await accion();
            return boton;
        }

        // ── Navegación ────────────────────────────────────────────────────────

        private async void OnVolverClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        // ── Tareas ─────────────────────────────────────────────────────────────

        private async void OnAgregarClicked(object sender, EventArgs e)
        {
            string? titulo = await PedirTextoAsync("Título de la tarea", "");
            if (string.IsNullOrWhiteSpace(titulo)) return;

            string? descripcion = await PedirTextoAsync("Descripción", "");
            if (descripcion is null) return;

            DateTime? fecha = await PedirFechaAsync("Fecha límite (dd/mm/aaaa · vacío = sin fecha)", null);
            if (fecha is null) return;

            await EjecutarCambioAsync(
                () => AdminService.InsertarTareaAsync(_usuario.Id, titulo, descripcion, fecha, EstadoTarea.Pendiente),
                "Tarea creada.");
        }

        private async Task EditarTareaAsync(AdminTarea tarea)
        {
            string? titulo = await PedirTextoAsync("Título", tarea.Titulo);
            if (string.IsNullOrWhiteSpace(titulo)) return;

            string? descripcion = await PedirTextoAsync("Descripción", tarea.Descripcion);
            if (descripcion is null) return;

            string? estado = await PedirEstadoAsync(tarea.Estado);
            if (estado is null) return;

            DateTime? fecha = await PedirFechaAsync(
                "Fecha límite (dd/mm/aaaa · vacío = sin fecha)", tarea.FechaVencimiento);
            if (fecha is null) return;

            await EjecutarCambioAsync(
                () => AdminService.ActualizarTareaAsync(_usuario.Id, tarea.Id, titulo, descripcion, fecha, estado),
                "Tarea actualizada.");
        }

        private async Task CambiarEstadoAsync(AdminTarea tarea)
        {
            string nuevo = tarea.EstaCompletada ? EstadoTarea.Pendiente : EstadoTarea.Completado;

            await EjecutarCambioAsync(
                () => AdminService.ActualizarTareaAsync(
                    _usuario.Id, tarea.Id, null, null, null, nuevo),
                nuevo == EstadoTarea.Completado ? "Tarea completada." : "Tarea reabierta.");
        }

        private async Task EliminarTareaAsync(AdminTarea tarea)
        {
            bool primera = await DisplayAlertAsync("Eliminar",
                $"¿Eliminar la tarea \"{tarea.Titulo}\" de {_usuario.Nombre}?", "Sí", "No");
            if (!primera) return;

            if (!await DisplayAlertAsync("Confirmación final",
                "Esta acción NO se puede deshacer.\n\n¿Eliminar la tarea definitivamente?",
                "Eliminar", "Cancelar"))
                return;

            await EjecutarCambioAsync(
                () => AdminService.EliminarTareaAsync(_usuario.Id, tarea.Id), "Tarea eliminada.");
        }

        // ── Cuenta ─────────────────────────────────────────────────────────────

        private async void OnEditarNombreClicked(object sender, EventArgs e)
        {
            string? nombre = await PedirTextoAsync("Nombre de la cuenta", _usuario.Nombre);
            if (string.IsNullOrWhiteSpace(nombre)) return;

            await EjecutarCambioSinRecargaAsync(
                async () =>
                {
                    await AdminService.ActualizarUsuarioAsync(_usuario.Id, nombre, null);
                    _usuario.Nombre = nombre;
                    LblNombreDet.Text = nombre;
                    LblInicialesDet.Text = _usuario.Iniciales;
                },
                "Nombre actualizado.");
        }

        private async void OnCambiarRolClicked(object sender, EventArgs e)
        {
            if (EsLaCuentaActiva())
            {
                await DisplayAlertAsync("Aviso",
                    "No se puede cambiar el rol de la cuenta con la que iniciaste sesión. " +
                    "Pídeselo a otro administrador.", "OK");
                return;
            }

            bool darAdmin = !_usuario.EsAdmin;
            string nuevoRol = darAdmin ? UserSession.EstadoAdmin : UserSession.EstadoUsuario;

            bool confirmado = await DisplayAlertAsync("Cambiar rol",
                darAdmin
                    ? $"¿Dar permisos de administrador a {_usuario.Nombre}?"
                    : $"¿Quitarle el rol de administrador a {_usuario.Nombre}?",
                darAdmin ? "Hacer admin" : "Quitar admin", "Cancelar");
            if (!confirmado) return;

            await EjecutarCambioSinRecargaAsync(
                async () =>
                {
                    await AdminService.ActualizarUsuarioAsync(_usuario.Id, null, nuevoRol);
                    _usuario.Rol = nuevoRol;
                    AplicarRolEnUI();
                },
                "Rol actualizado.");
        }

        private async void OnEliminarCuentaClicked(object sender, EventArgs e)
        {
            if (EsLaCuentaActiva())
            {
                await DisplayAlertAsync("Aviso", "No se puede eliminar la cuenta con la que iniciaste sesión.", "OK");
                return;
            }

            bool primera = await DisplayAlertAsync("Eliminar cuenta",
                $"¿Eliminar la cuenta de \"{_usuario.Nombre}\" con todas sus tareas y su acceso?", "Sí", "No");
            if (!primera) return;

            if (!await DisplayAlertAsync("Confirmación final",
                "Esta acción NO se puede deshacer.\n\n¿Eliminar la cuenta definitivamente?",
                "Eliminar", "Cancelar"))
                return;

            try
            {
                MostrarIndicador(true);
                await AdminService.EliminarUsuarioAsync(_usuario.Id);
                await DisplayAlertAsync("Listo", "Cuenta eliminada.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                MostrarIndicador(false);
            }
        }

        private bool EsLaCuentaActiva() => _usuario.Id == UserSession.CurrentUserId;

        // ── Auxiliares ────────────────────────────────────────────────────────

        private async Task<string?> PedirTextoAsync(string titulo, string actual)
            => await PedirTextoAsync(titulo, actual, null);

        private async Task<string?> PedirTextoAsync(string titulo, string actual, Keyboard? teclado)
            => await DisplayPromptAsync(titulo, "", "Aceptar", "Cancelar", null, 250, teclado, actual);

        private async Task<string?> PedirEstadoAsync(string actual)
        {
            var opciones = EstadoTarea.EsCompletado(actual)
                ? new[] { EstadoTarea.Completado, EstadoTarea.Pendiente }
                : new[] { EstadoTarea.Pendiente, EstadoTarea.Completado };

            var elegida = await DisplayActionSheetAsync("Estado", "Cancelar", null, opciones);
            return elegida is null or "Cancelar" ? null : elegida;
        }

        private async Task<DateTime?> PedirFechaAsync(string titulo, DateTime? principal)
        {
            var respuesta = await DisplayPromptAsync(
                titulo, principal?.ToLocalTime().ToString("dd/MM/yyyy") ?? "sin fecha");

            if (respuesta is null) return null;

            string texto = respuesta.Trim();
            if (texto.Length == 0 || string.Equals(texto, "sin fecha", StringComparison.OrdinalIgnoreCase))
                return null;

            string[] formatos = { "dd/MM/yyyy HH:mm", "dd/MM/yyyy", "yyyy-MM-dd HH:mm", "yyyy-MM-dd" };

            if (DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exacta) ||
                DateTime.TryParseExact(texto, formatos, CultureInfo.CurrentCulture, DateTimeStyles.None, out exacta))
                return exacta;

            if (DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var resultado) ||
                DateTime.TryParse(texto, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out resultado))
                return resultado;

            await DisplayAlertAsync("Dato inválido",
                "Usa el formato dd/mm/aaaa o dd/mm/aaaa hh:mm, o déjalo vacío para dejarla sin fecha.", "OK");
            return null;
        }

        private async Task EjecutarCambioAsync(Func<Task> accion, string mensajeOk)
        {
            try
            {
                await accion();
                await RefrescarSiEsMiCuenta();
                await RecargarAsync();
                await DisplayAlertAsync("Listo", mensajeOk, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async Task EjecutarCambioSinRecargaAsync(Func<Task> accion, string mensajeOk)
        {
            try
            {
                MostrarIndicador(true);
                await accion();
                await RefrescarSiEsMiCuenta();
                await DisplayAlertAsync("Listo", mensajeOk, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                MostrarIndicador(false);
            }
        }

        /// <summary>
        /// Si el panel está tocando la cuenta con la que se inició sesión, su copia
        /// local puede quedarse desfasada hasta el próximo ciclo. Se adelanta una
        /// sincronización para que la lista de la pantalla principal refleje el
        /// cambio en cuanto se vuelve atrás.
        /// </summary>
        private async Task RefrescarSiEsMiCuenta()
        {
            if (!EsLaCuentaActiva()) return;
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            try
            {
                await ServicioFondo.SincronizarEnSegundoPlanoAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminDetallePage: no se pudo refrescar la cuenta activa: {ex.Message}");
            }
        }
    }
}
