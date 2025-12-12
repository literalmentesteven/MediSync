using MediSync.Models;
using System.Net.Http.Json;
using System.Collections.ObjectModel;

namespace MediSync.Views;

public partial class DoctorsPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private ObservableCollection<Usuario> _doctores = new();

    public DoctorsPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await CargarDoctores();
    }

    private async Task CargarDoctores()
    {
        try
        {
            // Gestión de estado de carga
            LoadingSpinner.IsRunning = true;
            DoctorsCollection.IsVisible = false;
            EmptyLabel.IsVisible = false;

            var docs = await _httpClient.GetFromJsonAsync<List<Usuario>>("api/doctores");
            
            if (docs != null && docs.Count > 0)
            {
                _doctores = new ObservableCollection<Usuario>(docs);
                DoctorsCollection.ItemsSource = _doctores;
                DoctorsCollection.IsVisible = true;
            }
            else
            {
                EmptyLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Conexión", $"No se pudo obtener el directorio médico: {ex.Message}", "OK");
            EmptyLabel.IsVisible = true;
        }
        finally
        {
            LoadingSpinner.IsRunning = false;
        }
    }

    private async void OnViewProfileClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var doctor = button?.BindingContext as Usuario;

        if (doctor != null)
        {
            // Visualización rápida de contacto
            string mensaje = $"" +
                $"📞 Teléfono: {doctor.Telefono}\n" +
                $"🎂 Edad: {doctor.Edad} años\n" +
                $"📅 Fecha Nac: {doctor.FechaNacimiento:dd/MM/yyyy}\n" +
                $"🆔 ID Sistema: {doctor.IdUsuario}";

            await DisplayAlert($"Contacto: {doctor.NombreCompleto}", mensaje, "Cerrar");
        }
    }
}
