using MediSync.Models;
using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace MediSync.Views;

public partial class LabProcessingPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private Examen? _currentExam;
    private FileResult? _selectedFileResult; 

    public LabProcessingPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };
    }

    private async void OnPageLoaded(object sender, EventArgs e) => await CargarPendientes();

    private async Task CargarPendientes()
    {
        try {
            var all = await _httpClient.GetFromJsonAsync<List<Examen>>("api/examenes");
            if (all != null) {
                // Priorización: Urgentes primero
                var pendientes = all.Where(x => x.Estado == "Pendiente")
                                    .OrderByDescending(x => x.EsUrgente)
                                    .ToList();
                PendingList.ItemsSource = new ObservableCollection<Examen>(pendientes);
            }
        } catch { }
    }

    private void OnExamSelected(object sender, SelectionChangedEventArgs e)
    {
        _currentExam = e.CurrentSelection.FirstOrDefault() as Examen;
        if (_currentExam == null) return;

        WorkPanel.IsVisible = true;
        LblWorkTitle.Text = $"{_currentExam.TipoExamen} - {_currentExam.PacienteNombre}";
        LblDrRequest.Text = $"Solicitado por: {_currentExam.NombreDoctor}";
        
        TxtResultados.Text = "";
        CheckCritico.IsChecked = false;
        LblFileName.Text = "Ningún archivo seleccionado";
        _selectedFileResult = null; 
    }

    private async void OnUploadPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccione el reporte PDF oficial",
                FileTypes = FilePickerFileType.Pdf
            });

            if (result != null)
            {
                _selectedFileResult = result;
                LblFileName.Text = $"✅ {result.FileName} (Listo para cargar)";
            }
        }
        catch 
        { 
            await DisplayAlert("Error de I/O", "No se pudo acceder al archivo seleccionado.", "OK"); 
        }
    }

    private async void OnSubmitResult(object sender, EventArgs e)
    {
        if (_currentExam == null) return;

        // Validación de Integridad
        if (string.IsNullOrWhiteSpace(TxtResultados.Text) && _selectedFileResult == null) 
        { 
            await DisplayAlert("Validación", "Debe ingresar resultados manuales o adjuntar un PDF oficial.", "OK"); 
            return; 
        }

        string nombreArchivoServidor = "";

        // 1. Carga de Archivo (Upload)
        if (_selectedFileResult != null)
        {
            try 
            {
                using var stream = await _selectedFileResult.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var formContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
                
                formContent.Add(fileContent, "file", _selectedFileResult.FileName);

                var responseUpload = await _httpClient.PostAsync($"api/examenes/{_currentExam.Id}/upload", formContent);
                
                if (responseUpload.IsSuccessStatusCode)
                {
                    var jsonResponse = await responseUpload.Content.ReadFromJsonAsync<UploadResponse>();
                    if (jsonResponse != null) nombreArchivoServidor = jsonResponse.url;
                }
                else
                {
                    var errorMsg = await responseUpload.Content.ReadAsStringAsync();
                    await DisplayAlert("Error del Servidor", $"Fallo en la carga del PDF.\nDetalle: {errorMsg}", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Red", $"Interrupción durante la carga: {ex.Message}", "OK");
                return;
            }
        }

        // 2. Persistencia de Datos
        try {
            _currentExam.DatosResultado = TxtResultados.Text;
            _currentExam.EsCritico = CheckCritico.IsChecked;
            
            if (!string.IsNullOrEmpty(nombreArchivoServidor))
                _currentExam.ArchivoPdfUrl = nombreArchivoServidor;
            else if (string.IsNullOrEmpty(_currentExam.ArchivoPdfUrl))
                _currentExam.ArchivoPdfUrl = ""; 

            _currentExam.Estado = "Enviado"; 
            
            var response = await _httpClient.PutAsJsonAsync($"api/examenes/{_currentExam.Id}", _currentExam);
            
            if (response.IsSuccessStatusCode) {
                await DisplayAlert("Proceso Finalizado", "Resultados enviados exitosamente.", "Aceptar");
                WorkPanel.IsVisible = false;
                await CargarPendientes();
            }
        } 
        catch { await DisplayAlert("Error", "No se pudo actualizar el registro en base de datos.", "OK"); }
    }
}

public class UploadResponse { public string url { get; set; } = string.Empty; }
