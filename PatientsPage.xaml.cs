using MediSync.Models;
using MediSync.Helpers;
using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace MediSync.Views;

public partial class PatientsPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private ObservableCollection<Paciente> _allPacientes = new();
    private List<Usuario> _listaDoctores = new();
    private Paciente? _selectedPaciente;
    
    // Flags de Estado de UI
    private bool _isEditing = false;
    private bool _isBooking = false;

    public PatientsPage() 
    {
        InitializeComponent();
        
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };

        // Configuración de visibilidad según privilegios
        BtnAddPatient.IsVisible = UserInfo.IsAdminOrSuper;
    }

    private async void OnPageLoaded(object sender, EventArgs e) 
    {
        await CargarPacientes();
        await CargarDoctoresParaPicker();
    }

    #region Gestión de Datos y UI

    private async Task CargarPacientes()
    {
        try 
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var pacientes = await _httpClient.GetFromJsonAsync<List<Paciente>>("api/pacientes", options);
            
            if (pacientes != null) 
            {
                // Filtro de privacidad: Doctores solo ven sus pacientes asignados
                if (UserInfo.Rol == "Doctor")
                {
                    pacientes = pacientes.Where(p => p.DoctorAsignado == UserInfo.NombreUsuario).ToList();
                }

                _allPacientes = new ObservableCollection<Paciente>(pacientes);
                PatientsList.ItemsSource = _allPacientes;
            }
        } 
        catch (Exception ex) 
        { 
            await DisplayAlert("Error de Sincronización", $"Fallo al obtener listado de pacientes: {ex.Message}", "OK"); 
        }
    }

    private async Task CargarDoctoresParaPicker()
    {
        try 
        {
            var docs = await _httpClient.GetFromJsonAsync<List<Usuario>>("api/doctores");
            if (docs != null) _listaDoctores = docs;
        } 
        catch { /* Fallo silencioso no crítico */ }
    }

    private void OnPatientSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedPaciente = e.CurrentSelection.FirstOrDefault() as Paciente;
        
        if (_selectedPaciente == null) 
        { 
            DetailPanel.IsVisible = false; 
            EmptyStateLabel.IsVisible = true; 
            return; 
        }

        EmptyStateLabel.IsVisible = false; 
        DetailPanel.IsVisible = true;
        
        UpdateDetailUI();
    }

    private void UpdateDetailUI()
    {
        if (_selectedPaciente == null) return;

        // Binding manual de datos demográficos
        LblNombre.Text = _selectedPaciente.NombreCompleto;
        LblIdLegal.Text = $"ID: {_selectedPaciente.IdLegal}";
        
        var parts = _selectedPaciente.NombreCompleto.Split(' ');
        string ini = parts.Length > 0 ? parts[0][0].ToString() : "";
        if (parts.Length > 1) ini += parts[1][0].ToString();
        LblIniciales.Text = ini.ToUpper();

        LblEdad.Text = $"{_selectedPaciente.Edad} años";
        LblPeso.Text = $"{_selectedPaciente.Peso} kg";
        LblAltura.Text = $"{_selectedPaciente.Altura} m";
        LblSexo.Text = _selectedPaciente.Sexo;
        LblDireccion.Text = _selectedPaciente.Direccion;
        LblTelefono.Text = _selectedPaciente.Telefono;

        // Lógica de Estado de Cita
        if (_selectedPaciente.EstadoCita == "Sin Cita")
        {
            LblDoctorAsignado.Text = "Ninguno";
            BadgeEstado.BackgroundColor = Color.FromArgb("#F5F5F5"); 
            LblEstado.Text = "SIN CITA"; 
            LblEstado.TextColor = Colors.Gray;
            
            BtnAsistio.IsVisible = false; 
            BtnAusente.IsVisible = false;
        }
        else
        {
            LblDoctorAsignado.Text = _selectedPaciente.DoctorAsignado;
            LblEstado.Text = _selectedPaciente.EstadoCita.ToUpper();
            
            // Feedback visual de estado
            if(_selectedPaciente.EstadoCita == "Asistió") 
            { 
                BadgeEstado.BackgroundColor = Color.FromArgb("#E8F5E9"); 
                LblEstado.TextColor = Color.FromArgb("#388E3C"); 
            }
            else if(_selectedPaciente.EstadoCita == "Ausente") 
            { 
                BadgeEstado.BackgroundColor = Color.FromArgb("#FFEBEE"); 
                LblEstado.TextColor = Color.FromArgb("#D32F2F"); 
            }
            else // Pendiente
            { 
                BadgeEstado.BackgroundColor = Color.FromArgb("#FFF3E0"); 
                LblEstado.TextColor = Color.FromArgb("#FF9800"); 
            }

            // Regla de Negocio: Botones de asistencia solo activos si la cita es actual
            // (Tolerancia de 30 minutos previos)
            bool esPendiente = _selectedPaciente.EstadoCita == "Pendiente";
            bool tienePermiso = UserInfo.IsAdminOrSuper; 
            bool esHoraCita = DateTime.Now >= _selectedPaciente.HoraCita.AddMinutes(-30);
            
            BtnAsistio.IsVisible = esPendiente && tienePermiso && esHoraCita;
            BtnAusente.IsVisible = esPendiente && tienePermiso && esHoraCita;
        }

        // Configuración de Historial
        if (string.IsNullOrWhiteSpace(_selectedPaciente.HistoriaClinica))
        {
            BtnHistory.Text = "📝 Crear Historial";
            BtnHistory.BackgroundColor = Color.FromArgb("#FF9800");
        }
        else
        {
            BtnHistory.Text = "📂 Acceder Historial";
            BtnHistory.BackgroundColor = Color.FromArgb("#00B2CA");
        }

        BtnEditPatient.IsVisible = UserInfo.IsAdminOrSuper;
        BtnHistory.IsVisible = (UserInfo.Rol == "Doctor" || UserInfo.Rol == "Superusuario");
    }

    #endregion

    #region Lógica Transaccional (CRUD y Citas)

    private void LlenarPickerDoctores()
    {
        PickerDoctor.Items.Clear();
        foreach(var doc in _listaDoctores) 
            PickerDoctor.Items.Add(doc.NombreCompleto);
    }

    private void OnAddPatientClicked(object sender, EventArgs e)
    {
        _isEditing = false; 
        _isBooking = false;
        
        ModalTitle.Text = "Registro de Nuevo Paciente";
        TxtIdLegal.Text = "Generación Automática"; 
        
        // Reset de formulario
        TxtNombre.Text = ""; TxtEdad.Text = ""; TxtPeso.Text = ""; TxtAltura.Text = ""; TxtDireccion.Text = ""; TxtTelefono.Text = "";
        PickerSexo.SelectedIndex = -1;
        
        SectionPatientData.IsVisible = true;
        SectionCita.IsVisible = false;
        ModalOverlay.IsVisible = true;
    }

    private void OnNewAppointmentClicked(object sender, EventArgs e)
    {
        if (_selectedPaciente == null) return;
        
        _isEditing = false; 
        _isBooking = true; 
        
        ModalTitle.Text = $"Programar Cita: {_selectedPaciente.NombreCompleto}";
        
        LlenarPickerDoctores();
        PickerDate.Date = DateTime.Now;
        PickerTime.Time = DateTime.Now.TimeOfDay;

        SectionPatientData.IsVisible = false;
        SectionCita.IsVisible = true;
        ModalOverlay.IsVisible = true;
    }

    private void OnEditPatientClicked(object sender, EventArgs e)
    {
        if (_selectedPaciente == null) return;
        
        _isEditing = true; 
        _isBooking = false;
        
        ModalTitle.Text = "Actualizar Información";

        TxtNombre.Text = _selectedPaciente.NombreCompleto;
        TxtIdLegal.Text = _selectedPaciente.IdLegal;
        TxtEdad.Text = _selectedPaciente.Edad.ToString();
        TxtPeso.Text = _selectedPaciente.Peso.ToString();
        TxtAltura.Text = _selectedPaciente.Altura.ToString();
        TxtDireccion.Text = _selectedPaciente.Direccion;
        TxtTelefono.Text = _selectedPaciente.Telefono;
        PickerSexo.SelectedItem = _selectedPaciente.Sexo;
        
        SectionPatientData.IsVisible = true;
        SectionCita.IsVisible = false; 
        ModalOverlay.IsVisible = true;
    }

    private void CloseModal(object sender, EventArgs e) => ModalOverlay.IsVisible = false;

    private async void SavePatient(object sender, EventArgs e)
    {
        try
        {
            // CASO 1: AGENDAR CITA
            if (_isBooking) 
            {
                if (PickerDoctor.SelectedIndex == -1) 
                { 
                    await DisplayAlert("Validación", "Debe seleccionar un médico tratante.", "OK"); 
                    return; 
                }
                
                DateTime fechaHora = PickerDate.Date + PickerTime.Time;
                
                var nuevaCita = new Paciente
                {
                    NombreCompleto = _selectedPaciente!.NombreCompleto,
                    IdLegal = _selectedPaciente.IdLegal, 
                    Edad = _selectedPaciente.Edad, 
                    Peso = _selectedPaciente.Peso, 
                    Altura = _selectedPaciente.Altura, 
                    Sexo = _selectedPaciente.Sexo, 
                    Direccion = _selectedPaciente.Direccion, 
                    Telefono = _selectedPaciente.Telefono,
                    DoctorAsignado = PickerDoctor.SelectedItem.ToString(),
                    EstadoCita = "Pendiente",
                    HoraCita = fechaHora,
                    HistoriaClinica = _selectedPaciente.HistoriaClinica
                };

                var response = await _httpClient.PostAsJsonAsync("api/pacientes", nuevaCita);
                
                if (response.IsSuccessStatusCode) 
                { 
                    await CargarPacientes(); 
                    ModalOverlay.IsVisible = false; 
                    await DisplayAlert("Éxito", "La cita ha sido agendada correctamente.", "Aceptar"); 
                }
                else
                {
                    // Manejo de Error 400 (Conflicto de Agenda)
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try 
                    {
                        using var doc = JsonDocument.Parse(errorContent);
                        string mensajeError = doc.RootElement.GetProperty("message").GetString();
                        await DisplayAlert("Conflicto de Horario", mensajeError, "Entendido");
                    }
                    catch
                    {
                        await DisplayAlert("Error", "El servidor rechazó la solicitud.", "OK");
                    }
                }
            }
            // CASO 2: EDITAR PACIENTE
            else if (_isEditing && _selectedPaciente != null)
            {
                if (string.IsNullOrWhiteSpace(TxtNombre.Text)) return;
                
                int.TryParse(TxtEdad.Text, out int edad); 
                double.TryParse(TxtPeso.Text, out double peso); 
                double.TryParse(TxtAltura.Text, out double altura);

                _selectedPaciente.NombreCompleto = TxtNombre.Text;
                _selectedPaciente.Edad = edad; 
                _selectedPaciente.Peso = peso; 
                _selectedPaciente.Altura = altura;
                _selectedPaciente.Sexo = PickerSexo.SelectedItem?.ToString() ?? "";
                _selectedPaciente.Direccion = TxtDireccion.Text; 
                _selectedPaciente.Telefono = TxtTelefono.Text;

                var response = await _httpClient.PutAsJsonAsync($"api/pacientes/{_selectedPaciente.Id}", _selectedPaciente);
                
                if (response.IsSuccessStatusCode) 
                { 
                    UpdateDetailUI(); 
                    ModalOverlay.IsVisible = false; 
                    await DisplayAlert("Éxito", "Registro actualizado.", "OK"); 
                }
            }
            // CASO 3: NUEVO PACIENTE
            else
            {
                if (string.IsNullOrWhiteSpace(TxtNombre.Text)) return;
                
                int.TryParse(TxtEdad.Text, out int edad); 
                double.TryParse(TxtPeso.Text, out double peso); 
                double.TryParse(TxtAltura.Text, out double altura);

                var nuevo = new Paciente
                {
                    NombreCompleto = TxtNombre.Text,
                    Edad = edad, 
                    Peso = peso, 
                    Altura = altura,
                    Sexo = PickerSexo.SelectedItem?.ToString() ?? "",
                    Direccion = TxtDireccion.Text, 
                    Telefono = TxtTelefono.Text,
                    DoctorAsignado = "",
                    EstadoCita = "Sin Cita",
                    HoraCita = DateTime.MinValue,
                    HistoriaClinica = ""
                };

                var response = await _httpClient.PostAsJsonAsync("api/pacientes", nuevo);
                
                if (response.IsSuccessStatusCode) 
                { 
                    await CargarPacientes(); 
                    ModalOverlay.IsVisible = false; 
                    await DisplayAlert("Éxito", "Paciente registrado en la base de datos.", "OK"); 
                }
            }
        }
        catch (Exception ex) 
        { 
            await DisplayAlert("Excepción", $"Error inesperado: {ex.Message}", "OK"); 
        }
    }

    // Control de Asistencia
    private async void OnAsistioClicked(object sender, EventArgs e) => await ActualizarEstado("Asistió");
    private async void OnAusenteClicked(object sender, EventArgs e) => await ActualizarEstado("Ausente");

    private async Task ActualizarEstado(string nuevoEstado)
    {
        if (_selectedPaciente == null) return;
        
        _selectedPaciente.EstadoCita = nuevoEstado;
        await _httpClient.PutAsJsonAsync($"api/pacientes/{_selectedPaciente.Id}", _selectedPaciente);
        
        UpdateDetailUI();
        
        var index = _allPacientes.IndexOf(_selectedPaciente);
        if(index >= 0) _allPacientes[index] = _selectedPaciente;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) 
            PatientsList.ItemsSource = _allPacientes;
        else 
            PatientsList.ItemsSource = _allPacientes.Where(p => 
                p.NombreCompleto.ToLower().Contains(e.NewTextValue.ToLower())
            ).ToList();
    }

    #endregion

    #region Gestión de Historial Médico (SOAP)
    
    private void OnHistoryClicked(object sender, EventArgs e)
    {
        if (_selectedPaciente == null) return;
        LblHistoryPatientName.Text = $"Paciente: {_selectedPaciente.NombreCompleto}";
        TxtHistoryEditor.Text = _selectedPaciente.HistoriaClinica;
        HistoryModalOverlay.IsVisible = true;
    }

    private void CloseHistoryModal(object sender, EventArgs e) => HistoryModalOverlay.IsVisible = false;

    private async void SaveHistory(object sender, EventArgs e)
    {
        if (_selectedPaciente == null) return;

        try 
        {
            _selectedPaciente.HistoriaClinica = TxtHistoryEditor.Text;
            var response = await _httpClient.PutAsJsonAsync($"api/pacientes/{_selectedPaciente.Id}", _selectedPaciente);
            
            if (response.IsSuccessStatusCode) 
            {
                HistoryModalOverlay.IsVisible = false;
                UpdateDetailUI();
                await DisplayAlert("Guardado", "Expediente clínico actualizado.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo sincronizar el expediente.", "OK");
            }
        } 
        catch 
        { 
            await DisplayAlert("Error", "Error de comunicación con el servidor.", "OK"); 
        }
    }

    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        if (TxtHistoryEditor != null) TxtHistoryEditor.FontSize = e.NewValue;
    }

    private void OnFontChanged(object sender, EventArgs e)
    {
        if (TxtHistoryEditor == null || FontPicker.SelectedItem == null) return;
        var selection = FontPicker.SelectedItem as string;
        
        TxtHistoryEditor.FontFamily = selection == "Monospace (Código)" ? "Monospace" : 
                                      selection == "Serif (Formal)" ? "Serif" : 
                                      "OpenSansRegular";
    }

    private void OnInsertDate(object sender, EventArgs e) => 
        TxtHistoryEditor.Text += $"\n--- {DateTime.Now:dd/MM/yyyy HH:mm} ---\n";

    private void OnInsertTemplate(object sender, EventArgs e) => 
        TxtHistoryEditor.Text += "\nS (Subjetivo): \nO (Objetivo): \nA (Análisis): \nP (Plan): \n";
    
    #endregion
}
