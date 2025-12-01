using MediSync.Models;
using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Net.Http.Headers; // IMPORTANTE: Para configurar el tipo de archivo (PDF)

namespace MediSync.Views;

public partial class LabProcessingPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private Examen? _currentExam;
    
    // Guardamos el archivo seleccionado en memoria
    private FileResult? _selectedFileResult; 

    public LabProcessingPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        // Recuerda: Si usas Android Emulator usa 10.0.2.2 en vez de localhost
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };
    }

    private async void OnPageLoaded(object sender, EventArgs e) => await CargarPendientes();

    private async Task CargarPendientes()
    {
        try {
            var all = await _httpClient.GetFromJsonAsync<List<Examen>>("api/examenes");
            if (all != null) {
                var pendientes = all.Where(x => x.Estado == "Pendiente").OrderByDescending(x => x.EsUrgente).ToList();
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
        
        // Limpiamos la selección anterior
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
                PickerTitle = "Seleccione el reporte PDF",
                FileTypes = FilePickerFileType.Pdf
            });

            if (result != null)
            {
                _selectedFileResult = result;
                LblFileName.Text = $"✅ {result.FileName} (Listo para subir)";
            }
        }
        catch { await DisplayAlert("Error", "No se pudo cargar el archivo localmente.", "OK"); }
    }

    private async void OnSubmitResult(object sender, EventArgs e)
    {
        if (_currentExam == null) return;

        if (string.IsNullOrWhiteSpace(TxtResultados.Text) && _selectedFileResult == null) 
        { 
            await DisplayAlert("Atención", "Debe ingresar resultados manuales o seleccionar un PDF.", "OK"); 
            return; 
        }

        string nombreArchivoServidor = "";

        // =========================================================
        // 1. SUBIDA DEL ARCHIVO AL SERVIDOR
        // =========================================================
        if (_selectedFileResult != null)
        {
            try 
            {
                // Leemos el archivo a memoria RAM primero (más seguro)
                using var stream = await _selectedFileResult.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Preparamos el paquete para enviar
                var formContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
                
                // "file" debe coincidir con el nombre en Program.cs
                formContent.Add(fileContent, "file", _selectedFileResult.FileName);

                // Enviamos al endpoint que acabamos de arreglar en Program.cs
                var responseUpload = await _httpClient.PostAsync($"api/examenes/{_currentExam.Id}/upload", formContent);
                
                if (responseUpload.IsSuccessStatusCode)
                {
                    var jsonResponse = await responseUpload.Content.ReadFromJsonAsync<UploadResponse>();
                    if (jsonResponse != null) nombreArchivoServidor = jsonResponse.url;
                }
                else
                {
                    var errorMsg = await responseUpload.Content.ReadAsStringAsync();
                    await DisplayAlert("Error del Servidor", $"No se pudo subir el PDF.\nDetalle: {errorMsg}", "OK");
                    return; // Cancelamos si falla la subida
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de Conexión", $"Falló el envío: {ex.Message}", "OK");
                return;
            }
        }

        // =========================================================
        // 2. ACTUALIZACIÓN DE DATOS
        // =========================================================
        try {
            _currentExam.DatosResultado = TxtResultados.Text;
            _currentExam.EsCritico = CheckCritico.IsChecked;
            
            // Si subimos archivo, guardamos el nombre nuevo. Si no, dejamos el que estaba.
            if (!string.IsNullOrEmpty(nombreArchivoServidor))
                _currentExam.ArchivoPdfUrl = nombreArchivoServidor;
            else if (string.IsNullOrEmpty(_currentExam.ArchivoPdfUrl))
                _currentExam.ArchivoPdfUrl = ""; 

            _currentExam.Estado = "Enviado"; 
            
            var response = await _httpClient.PutAsJsonAsync($"api/examenes/{_currentExam.Id}", _currentExam);
            
            if (response.IsSuccessStatusCode) {
                await DisplayAlert("Éxito", $"Resultados enviados correctamente.", "OK");
                WorkPanel.IsVisible = false;
                await CargarPendientes();
            }
        } catch { await DisplayAlert("Error", "Fallo al guardar los datos del examen.", "OK"); }
    }
}

public class UploadResponse { public string url { get; set; } = string.Empty; }