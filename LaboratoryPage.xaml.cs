using MediSync.Models;
using MediSync.Helpers;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Microsoft.Maui.ApplicationModel; // Necesario para el Launcher

namespace MediSync.Views;

public partial class LaboratoryPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private ObservableCollection<Examen> _allExamenes = new();
    private Examen? _selectedExam;
    
    // LISTA PARA EL CHECKLIST
    public ObservableCollection<TipoExamenCheck> TiposExamenes { get; set; } = new ObservableCollection<TipoExamenCheck>
    {
        new TipoExamenCheck { Nombre = "Hemograma Completo" },
        new TipoExamenCheck { Nombre = "Perfil Lipídico" },
        new TipoExamenCheck { Nombre = "Glucosa en Ayunas" },
        new TipoExamenCheck { Nombre = "Urinálisis" },
        new TipoExamenCheck { Nombre = "Prueba COVID-19" },
        new TipoExamenCheck { Nombre = "Perfil Hepático" }
    };

    public LaboratoryPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        // NOTA: Si usas Android Emulator, recuerda que localhost es 10.0.2.2
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };
        
        CheckListExams.ItemsSource = TiposExamenes;
    }

    private async void OnPageLoaded(object sender, EventArgs e) 
    {
        await CargarExamenes();
        await CargarPacientes();
    }

    private async Task CargarExamenes()
    {
        try {
            var data = await _httpClient.GetFromJsonAsync<List<Examen>>("api/examenes");
            if (data != null) {
                if (UserInfo.Rol == "Doctor") data = data.Where(e => e.DoctorSolicitanteId == UserInfo.IdUsuario).ToList();
                _allExamenes = new ObservableCollection<Examen>(data);
                ExamsList.ItemsSource = _allExamenes;
            }
        } catch { }
    }

    private async Task CargarPacientes()
    {
        try {
            var data = await _httpClient.GetFromJsonAsync<List<Paciente>>("api/pacientes");
            if(data != null) {
                if (UserInfo.Rol == "Doctor") data = data.Where(p => p.DoctorAsignado == UserInfo.NombreUsuario).ToList();
                PickerPatient.Items.Clear();
                foreach(var p in data) PickerPatient.Items.Add(p.NombreCompleto);
            }
        } catch {}
    }

    private void OnExamSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedExam = e.CurrentSelection.FirstOrDefault() as Examen;
        if (_selectedExam == null) return;

        EmptyState.IsVisible = false;
        ReportPanel.IsVisible = true;

        LblExamTitle.Text = _selectedExam.TipoExamen.ToUpper();
        LblPatientName.Text = $"Paciente: {_selectedExam.PacienteNombre}";
        LblDate.Text = _selectedExam.Fecha.ToString("dd/MM/yyyy");

        if (_selectedExam.Estado == "Pendiente") {
            PendingAlert.IsVisible = true;
            ResultsGrid.ItemsSource = null;
            PdfAvailableAlert.IsVisible = false;
            BtnPrint.IsVisible = false;
            LblResultadosTexto.Text = "";
        } else {
            PendingAlert.IsVisible = false;
            
            if (!string.IsNullOrEmpty(_selectedExam.ArchivoPdfUrl))
            {
                PdfAvailableAlert.IsVisible = true;
                ResultsGrid.ItemsSource = null; 
                // Cambiamos el texto para indicar acción externa
                BtnPrint.Text = "📄 Abrir PDF Externo"; 
            }
            else
            {
                PdfAvailableAlert.IsVisible = false;
                ParseAndShowResults(_selectedExam.DatosResultado, ResultsGrid);
                BtnPrint.Text = "🖨️ Ver Hoja Impresa";
            }
            BtnPrint.IsVisible = true;
        }
    }

    private void ParseAndShowResults(string rawData, CollectionView targetGrid)
    {
        var lista = new List<ResultadoDetalle>();
        if (string.IsNullOrEmpty(rawData)) { targetGrid.ItemsSource = null; return; }

        var lineas = rawData.Split('|');
        foreach (var linea in lineas) {
            try {
                var partes = linea.Split(':');
                if (partes.Length < 2) continue;
                string nombre = partes[0].Trim();
                string resto = partes[1].Trim();
                string valor = resto;
                string referencia = "-";
                int idxPar = resto.IndexOf('(');
                if (idxPar > -1) {
                    valor = resto.Substring(0, idxPar).Trim();
                    referencia = resto.Substring(idxPar).Replace("(", "").Replace(")", "").Trim();
                }
                Color color = Colors.Black;
                if (linea.Contains("[ALTO]") || linea.Contains("[BAJO]")) { 
                    color = Colors.Red; 
                    valor = valor.Replace("[ALTO]", "▲").Replace("[BAJO]", "▼"); 
                }
                lista.Add(new ResultadoDetalle { Parametro = nombre, Valor = valor, Referencia = referencia, ColorValor = color });
            } catch { }
        }
        targetGrid.ItemsSource = lista;
    }

    // =========================================================================
    // MODIFICADO: Lógica para abrir PDF en navegador externo
    // =========================================================================
    private async void OnPrintClicked(object sender, EventArgs e)
    {
        if (_selectedExam == null) return;

        // CASO 1: Es un PDF (URL) -> Abrir en Navegador del Sistema
        if (!string.IsNullOrEmpty(_selectedExam.ArchivoPdfUrl))
        {
            try
            {
                string urlCompleta = _selectedExam.ArchivoPdfUrl;
                string baseUrl = "http://localhost:7151"; // OJO: Cambiar a tu IP o 10.0.2.2 si es Android Emulator

                // Si la URL es relativa (ej: "/pdf/archivo.pdf"), le pegamos el dominio
                if (!urlCompleta.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    if (urlCompleta.StartsWith("/"))
                        urlCompleta = baseUrl + urlCompleta;
                    else
                        urlCompleta = $"{baseUrl}/{urlCompleta}";
                }

                // Usamos Launcher para abrir el navegador predeterminado (Chrome, Edge, etc.)
                await Launcher.Default.OpenAsync(new Uri(urlCompleta));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el PDF: {ex.Message}", "OK");
            }
        }
        // CASO 2: Es un reporte generado (Datos texto) -> Usar el Modal Interno
        else
        {
            PrintPreviewModal.IsVisible = true;
            
            // Aseguramos que el WebView esté oculto y el contenedor nativo visible
            PdfViewer.IsVisible = false;
            DocumentContainer.IsVisible = true;

            // Llenamos los datos del reporte manual
            DocPatientName.Text = _selectedExam.PacienteNombre.ToUpper();
            DocExamType.Text = _selectedExam.TipoExamen.ToUpper();
            DocDate.Text = _selectedExam.Fecha.ToString("dd/MM/yyyy HH:mm");
            DocDoctorName.Text = string.IsNullOrEmpty(_selectedExam.NombreDoctor) ? "DR. GENERAL" : _selectedExam.NombreDoctor.ToUpper();
            LblFolio.Text = $"FOLIO: #{_selectedExam.Id:D6}";
            
            if (_selectedExam.Estado == "Pendiente") {
                DocPendingBanner.IsVisible = true;
                DocResultsGrid.ItemsSource = null;
            } else {
                DocPendingBanner.IsVisible = false;
                ParseAndShowResults(_selectedExam.DatosResultado, DocResultsGrid);
            }
        }
    }

    private void ClosePrintModal(object sender, EventArgs e)
    {
        PrintPreviewModal.IsVisible = false;
        PdfViewer.Source = "about:blank";
    }

    // --- NUEVA ORDEN ---
    private void OnNewOrderClicked(object sender, EventArgs e) => OrderModal.IsVisible = true;
    private void CloseOrderModal(object sender, EventArgs e) => OrderModal.IsVisible = false;

    private async void SendOrder(object sender, EventArgs e)
    {
        if (PickerPatient.SelectedIndex == -1) { await DisplayAlert("Error", "Seleccione paciente", "OK"); return; }
        
        var selectedExams = TiposExamenes.Where(x => x.IsSelected).ToList();
        if (selectedExams.Count == 0) { await DisplayAlert("Error", "Seleccione al menos un examen", "OK"); return; }

        string paciente = PickerPatient.SelectedItem.ToString();
        bool urgente = CheckUrgente.IsChecked;

        foreach (var tipo in selectedExams)
        {
            var nuevoExamen = new Examen
            {
                PacienteNombre = paciente,
                TipoExamen = tipo.Nombre,
                EsUrgente = urgente,
                Fecha = DateTime.Now,
                DoctorSolicitanteId = UserInfo.IdUsuario,
                NombreDoctor = UserInfo.NombreUsuario
            };
            await _httpClient.PostAsJsonAsync("api/examenes", nuevoExamen);
        }

        await CargarExamenes();
        OrderModal.IsVisible = false;
        await DisplayAlert("Éxito", "Órdenes enviadas.", "OK");
        foreach(var t in TiposExamenes) t.IsSelected = false;
        CheckUrgente.IsChecked = false;
    }
    
    private async void OnMarkReviewedClicked(object sender, EventArgs e) => await DisplayAlert("Revisado", "El examen ha sido archivado.", "OK");
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) 
    {
        string filtro = e.NewTextValue?.ToLower() ?? "";
        if (string.IsNullOrWhiteSpace(filtro)) ExamsList.ItemsSource = _allExamenes;
        else ExamsList.ItemsSource = new ObservableCollection<Examen>(_allExamenes.Where(x => x.PacienteNombre.ToLower().Contains(filtro) || x.TipoExamen.ToLower().Contains(filtro)).ToList());
    } 
}