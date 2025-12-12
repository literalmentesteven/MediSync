using System.Net.Http.Json;
using System.Text.Json;
using MediSync.Models;
using MediSync.Helpers;

namespace MediSync.Views;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public LoginPage() : this(new HttpClient()) { }

    public LoginPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    private void OnPageLoaded(object sender, EventArgs e)
    {
        // Animación ambiental del gradiente de fondo
        var animation = new Animation(v =>
        {
            BackgroundGradient.GradientStops[1].Offset = (float)v;
        }, 0, 1, Easing.SinInOut);

        animation.Commit(this, "GradientWave", 16, 5000, Easing.Linear, (v, c) => 
        {
            BackgroundGradient.GradientStops[1].Offset = 0;
        }, () => true);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryUsuario.Text) || string.IsNullOrWhiteSpace(EntryPassword.Text))
        {
            await DisplayAlert("Validación", "Credenciales requeridas.", "OK");
            return;
        }

        LoadingSpinner.IsRunning = true;
        this.IsEnabled = false;

        try
        {
            var loginReq = new LoginRequest 
            { 
                IdUsuario = EntryUsuario.Text, 
                Contraseña = EntryPassword.Text 
            };

            var response = await _httpClient.PostAsJsonAsync("api/login", loginReq);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(options);

                if (result != null)
                {
                    // Persistencia de sesión en memoria estática
                    UserInfo.Token = result.Token;
                    UserInfo.Rol = result.Rol;
                    UserInfo.NombreUsuario = result.NombreUsuario;
                    UserInfo.IdUsuario = EntryUsuario.Text;

                    // Transición a la Shell principal
                    if (Application.Current != null)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                }
            }
            else
            {
                await DisplayAlert("Acceso Denegado", "Usuario o contraseña incorrectos.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Conexión", $"No se pudo contactar al servidor de autenticación.\nDetalle: {ex.Message}", "OK");
        }
        finally
        {
            LoadingSpinner.IsRunning = false;
            this.IsEnabled = true;
        }
    }
}
