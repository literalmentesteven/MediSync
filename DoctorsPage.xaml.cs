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
            LoadingSpinner.IsRunning = true;
            DoctorsCollection.IsVisible = false;

            var docs = await _httpClient.GetFromJsonAsync<List<Usuario>>("api/doctores");
            
            if (docs != null && docs.Count > 0)
            {
                _doctores = new ObservableCollection<Usuario>(docs);
                DoctorsCollection.ItemsSource = _doctores;
                DoctorsCollection.IsVisible = true;
                EmptyLabel.IsVisible = false;
            }
            else
            {
                EmptyLabel.IsVisible = true;
            }
        }
        catch
        {
            await DisplayAlert("Error", "No se pudieron cargar los doctores.", "OK");
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
            string mensaje = $"" +
                $"📞 Teléfono: {doctor.Telefono}\n" +
                $"🎂 Edad: {doctor.Edad} años\n" +
                $"📅 Fecha Nac: {doctor.FechaNacimiento:dd/MM/yyyy}\n" +
                $"🆔 ID Sistema: {doctor.IdUsuario}";

            await DisplayAlert("Datos Privados", mensaje, "Cerrar");
        }
    }
}