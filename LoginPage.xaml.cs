// PASO 1: Importamos las librerías nuevas
using MediSync.Models; // Usar las clases que creamos
using System.Net.Http;
using System.Net.Http.Json; // El paquete que instalamos
using System.Text.Json; 

namespace MediSync
{
    public partial class LoginPage : ContentPage
    {
        private Color _acentoColor;
        private Color _bordeSuaveColor;
        
        // =================================================================
        // LÓGICA DE API
        // =================================================================

        // 1. Creamos el "teléfono" (HttpClient) estático para la app
        private static HttpClient _httpClient;

        // 2. ¡LA URL DE ROBERTO! (Versión Windows)
        //    Como tu app corre en Windows, SÍ puedes usar localhost.
        private readonly string _apiUrl = "http://localhost:5069";


        public LoginPage()
        {
            InitializeComponent(); 

            _acentoColor = (Color)this.Resources["ColorAcentoMediSync"];
            _bordeSuaveColor = (Color)this.Resources["ColorBordeSuave"];

            // 3. Inicializamos el HttpClient (simple)
            if (_httpClient == null)
            {
                // Como es 'http' y en Windows, no necesitamos
                // nada complicado.
                _httpClient = new HttpClient();
            }
        }

        // =================================================================
        // MÉTODOS VISUALES (Estos no cambian)
        // =================================================================

        private void OnPageLoaded(object sender, EventArgs e)
        {
            var animation = new Animation();
            animation.Add(0, 0.5, new Animation(v => {
                BackgroundGradient.StartPoint = new Point(v, v);
                BackgroundGradient.EndPoint = new Point(1 + v, 1 + v);
            }, 0, 1, Easing.SinInOut));
            animation.Add(0.5, 1, new Animation(v => {
                BackgroundGradient.StartPoint = new Point(v, v);
                BackgroundGradient.EndPoint = new Point(1 + v, 1 + v);
            }, 1, 0, Easing.SinInOut));
            animation.Commit(this, "GradientSlideAnimation", 16, 10000, Easing.Linear, null, () => true); 
        }

        private void OnEntryFocused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry && entry.Parent is Border parentBorder)
            {
                parentBorder.Stroke = _acentoColor;
                parentBorder.StrokeThickness = 2;
            }
        }

        private void OnEntryUnfocused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry && entry.Parent is Border parentBorder)
            {
                parentBorder.Stroke = _bordeSuaveColor;
                parentBorder.StrokeThickness = 1;
            }
        }

        // =================================================================
        // ¡MÉTODO DE LOGIN ACTUALIZADO!
        // =================================================================
        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            LoginButton.IsEnabled = false;
            // Podrías añadir un ActivityIndicator y ponerlo IsRunning = true

            try
            {
                // 1. Crear el objeto que enviaremos
                var loginRequest = new LoginRequest
                {
                    IdUsuario = IdEntry.Text,
                    Contraseña = PasswordEntry.Text
                };

                // 2. Definir la URL completa del endpoint (¡confirma esta ruta!)
                string loginUrl = $"{_apiUrl}/api/login"; 

                // 3. Llamar a la API de Roberto (POST)
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(loginUrl, loginRequest);

                // 4. Revisar la respuesta
                if (response.IsSuccessStatusCode)
                {
                    // ¡Éxito!
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                    // TODO: Guardar el 'loginResponse.Token'
                    // ...

                    // Navegar a la página principal
                    if (Application.Current != null)
                        Application.Current.Windows[0].Page = new AppShell();
                }
                else
                {
                    // Error de login (usuario/pass mal)
                    string error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error de Login", $"No se pudo ingresar. El servidor dijo: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                // Error de Conexión (ej. la URL está mal, el servidor de Roberto está apagado)
                await DisplayAlert("Error de Conexión", $"No se pudo conectar al servidor: {ex.Message}", "OK");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                // ActivityIndicator.IsRunning = false;
            }
        }

        private void OnForgotPasswordTapped(object sender, TappedEventArgs e)
        {
            DisplayAlert("Info", "Sección 'Olvidé Contraseña' en construcción.", "OK");
        }
    }
}