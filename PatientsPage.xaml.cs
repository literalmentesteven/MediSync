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
    
    // Flags para controlar qué estamos haciendo en el Modal
    private bool _isEditing = false;
    private bool _isBooking = false; // True = Agendando Cita, False = Creando/Editando Paciente

    public PatientsPage() 
    {
        InitializeComponent();
        
        // Inyección del HttpClient configurado
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };

        // PERMISOS INICIALES: Botón "Nuevo Paciente" solo para Admin/Super
        if (UserInfo.IsAdminOrSuper) 
        {
            BtnAddPatient.IsVisible = true;
        }
        else
        {
            BtnAddPatient.IsVisible = false;
        }
    }

    private async void OnPageLoaded(object sender, EventArgs e) 
    {
        await CargarPacientes();
        await CargarDoctoresParaPicker();
    }

    // --- CARGA DE DATOS ---

    private async Task CargarPacientes()
    {
        try 
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var pacientes = await _httpClient.GetFromJsonAsync<List<Paciente>>("api/pacientes", options);
            
            if (pacientes != null) 
            {
                // FILTRO POR ROL: Si es Doctor, solo ver sus pacientes
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
            await DisplayAlert("Error", $"No se pudieron cargar los pacientes: {ex.Message}", "OK"); 
        }
    }

    private async Task CargarDoctoresParaPicker()
    {
        try 
        {
            var docs = await _httpClient.GetFromJsonAsync<List<Usuario>>("api/doctores");
            if (docs != null) _listaDoctores = docs;
        } 
        catch { }
    }

    // --- SELECCIÓN DE PACIENTE ---

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

        // 1. Datos Básicos
        LblNombre.Text = _selectedPaciente.NombreCompleto;
        LblIdLegal.Text = $"ID: {_selectedPaciente.IdLegal}";
        
        // 2. Iniciales para el Avatar
        var parts = _selectedPaciente.NombreCompleto.Split(' ');
        string ini = parts.Length > 0 ? parts[0][0].ToString() : "";
        if (parts.Length > 1) ini += parts[1][0].ToString();
        LblIniciales.Text = ini.ToUpper();

        // 3. Datos Médicos y Contacto
        LblEdad.Text = $"{_selectedPaciente.Edad} años";
        LblPeso.Text = $"{_selectedPaciente.Peso} kg";
        LblAltura.Text = $"{_selectedPaciente.Altura} m";
        LblSexo.Text = _selectedPaciente.Sexo;
        LblDireccion.Text = _selectedPaciente.Direccion;
        LblTelefono.Text = _selectedPaciente.Telefono;

        // 4. Estado de la Cita y Doctor
        if (_selectedPaciente.EstadoCita == "Sin Cita")
        {
            LblDoctorAsignado.Text = "Ninguno";
            BadgeEstado.BackgroundColor = Color.FromArgb("#F5F5F5"); 
            LblEstado.Text = "SIN CITA"; 
            LblEstado.TextColor = Colors.Gray;
            
            // Ocultar botones de asistencia si no hay cita
            BtnAsistio.IsVisible = false; 
            BtnAusente.IsVisible = false;
        }
        else
        {
            LblDoctorAsignado.Text = _selectedPaciente.DoctorAsignado;
            LblEstado.Text = _selectedPaciente.EstadoCita.ToUpper();
            
            // Colores según estado
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

            // Visibilidad botones asistencia (Solo si es pendiente y usuario autorizado)
            bool esPendiente = _selectedPaciente.EstadoCita == "Pendiente";
            bool puedeMarcar = UserInfo.IsAdminOrSuper; // Solo admins marcan asistencia
            
            BtnAsistio.IsVisible = esPendiente && puedeMarcar;
            BtnAusente.IsVisible = esPendiente && puedeMarcar;
        }

        // 5. Configurar Botón Historial (Texto y Color)
        if (string.IsNullOrWhiteSpace(_selectedPaciente.HistoriaClinica))
        {
            BtnHistory.Text = "📝 Crear Historial";
            BtnHistory.BackgroundColor = Color.FromArgb("#FF9800"); // Naranja
        }
        else
        {
            BtnHistory.Text = "📂 Acceder Historial";
            BtnHistory.BackgroundColor = Color.FromArgb("#00B2CA"); // Cyan
        }

        // 6. Permisos Generales de Edición
        // Los doctores pueden ver pero no editar datos personales desde aquí
        BtnEditPatient.IsVisible = UserInfo.IsAdminOrSuper;
        
        // El historial solo lo ven Doctores y Superadmin
        BtnHistory.IsVisible = (UserInfo.Rol == "Doctor" || UserInfo.Rol == "Superusuario");
    }

    // --- ACCIONES CRUD (MODAL PRINCIPAL) ---

    private void LlenarPickerDoctores()
    {
        PickerDoctor.Items.Clear();
        foreach(var doc in _listaDoctores) 
        {
            PickerDoctor.Items.Add(doc.NombreCompleto);
        }
    }

    // A. CREAR NUEVO PACIENTE (Desde cero)
    private void OnAddPatientClicked(object sender, EventArgs e)
    {
        _isEditing = false; 
        _isBooking = false;
        
        ModalTitle.Text = "Nuevo Paciente";
        TxtIdLegal.Text = "Automático"; // Se genera en backend
        
        // Limpiar campos
        TxtNombre.Text = ""; TxtEdad.Text = ""; TxtPeso.Text = ""; TxtAltura.Text = ""; TxtDireccion.Text = ""; TxtTelefono.Text = "";
        PickerSexo.SelectedIndex = -1;
        
        // Configurar UI: Mostrar datos paciente, Ocultar datos cita
        SectionPatientData.IsVisible = true;
        SectionCita.IsVisible = false;
        
        ModalOverlay.IsVisible = true;
    }

    // B. AGENDAR NUEVA CITA (Para paciente seleccionado)
    private void OnNewAppointmentClicked(object sender, EventArgs e)
    {
        if (_selectedPaciente == null) return;
        
        _isEditing = false; 
        _isBooking = true; // Estamos en modo "Booking"
        
        ModalTitle.Text = $"Cita para {_selectedPaciente.NombreCompleto}";
        
        LlenarPickerDoctores();
        PickerDate.Date = DateTime.Now;
        PickerTime.Time = DateTime.Now.TimeOfDay;

        // Configurar UI: Ocultar datos paciente, Mostrar datos cita
        SectionPatientData.IsVisible = false;
        SectionCita.IsVisible = true;
        
        ModalOverlay.IsVisible = true;
    }

    // C. EDITAR PACIENTE EXISTENTE
    private void OnEditPatientClicked(object sender, EventArgs e)
    {
        if (_selectedPaciente == null) return;
        
        _isEditing = true; 
        _isBooking = false;
        
        ModalTitle.Text = "Editar Registro";

        // Llenar campos con datos actuales
        TxtNombre.Text = _selectedPaciente.NombreCompleto;
        TxtIdLegal.Text = _selectedPaciente.IdLegal;
        TxtEdad.Text = _selectedPaciente.Edad.ToString();
        TxtPeso.Text = _selectedPaciente.Peso.ToString();
        TxtAltura.Text = _selectedPaciente.Altura.ToString();
        TxtDireccion.Text = _selectedPaciente.Direccion;
        TxtTelefono.Text = _selectedPaciente.Telefono;
        PickerSexo.SelectedItem = _selectedPaciente.Sexo;
        
        // Configurar UI: Mostrar datos paciente, Ocultar datos cita
        SectionPatientData.IsVisible = true;
        SectionCita.IsVisible = false; 
        
        ModalOverlay.IsVisible = true;
    }

    private void CloseModal(object sender, EventArgs e) => ModalOverlay.IsVisible = false;

    private async void SavePatient(object sender, EventArgs e)
    {
        try
        {
            // CASO 1: AGENDAR CITA (Crear nuevo registro de tipo Cita copiando datos personales)
            if (_isBooking) 
            {
                if (PickerDoctor.SelectedIndex == -1) 
                { 
                    await DisplayAlert("Error", "Seleccione un doctor", "OK"); 
                    return; 
                }
                
                DateTime fechaHora = PickerDate.Date + PickerTime.Time;
                
                // Creamos un nuevo objeto Paciente (Cita) copiando los datos del seleccionado
                var nuevaCita = new Paciente
                {
                    NombreCompleto = _selectedPaciente!.NombreCompleto,
                    IdLegal = _selectedPaciente.IdLegal, // Mismo ID legal visual
                    Edad = _selectedPaciente.Edad, 
                    Peso = _selectedPaciente.Peso, 
                    Altura = _selectedPaciente.Altura, 
                    Sexo = _selectedPaciente.Sexo, 
                    Direccion = _selectedPaciente.Direccion, 
                    Telefono = _selectedPaciente.Telefono,
                    
                    // Datos nuevos de la cita
                    DoctorAsignado = PickerDoctor.SelectedItem.ToString(),
                    EstadoCita = "Pendiente",
                    HoraCita = fechaHora,
                    HistoriaClinica = _selectedPaciente.HistoriaClinica // Mantiene historial
                };

                var response = await _httpClient.PostAsJsonAsync("api/pacientes", nuevaCita);
                
                if (response.IsSuccessStatusCode) 
                { 
                    await CargarPacientes(); 
                    ModalOverlay.IsVisible = false; 
                    await DisplayAlert("Éxito", "Cita Agendada Correctamente", "OK"); 
                }
            }
            // CASO 2: EDITAR DATOS PACIENTE
            else if (_isEditing && _selectedPaciente != null)
            {
                if (string.IsNullOrWhiteSpace(TxtNombre.Text)) return;
                
                int.TryParse(TxtEdad.Text, out int edad); 
                double.TryParse(TxtPeso.Text, out double peso); 
                double.TryParse(TxtAltura.Text, out double altura);

                // Actualizamos el objeto en memoria
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
                    await DisplayAlert("Éxito", "Datos actualizados", "OK"); 
                }
            }
            // CASO 3: CREAR NUEVO PACIENTE (SIN CITA INICIAL)
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
                    
                    // Inicializar vacío/sin cita
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
                    await DisplayAlert("Éxito", "Paciente Registrado", "OK"); 
                }
            }
        }
        catch (Exception ex) 
        { 
            await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK"); 
        }
    }

    // --- ACCIONES RÁPIDAS (ASISTENCIA) ---

    private async void OnAsistioClicked(object sender, EventArgs e) => await ActualizarEstado("Asistió");
    private async void OnAusenteClicked(object sender, EventArgs e) => await ActualizarEstado("Ausente");

    private async Task ActualizarEstado(string nuevoEstado)
    {
        if (_selectedPaciente == null) return;
        
        _selectedPaciente.EstadoCita = nuevoEstado;
        await _httpClient.PutAsJsonAsync($"api/pacientes/{_selectedPaciente.Id}", _selectedPaciente);
        
        UpdateDetailUI();
        
        // Truco para refrescar el color en la lista
        var index = _allPacientes.IndexOf(_selectedPaciente);
        if(index >= 0) 
        {
            _allPacientes[index] = _selectedPaciente;
        }
    }

    // --- BÚSQUEDA ---

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) 
        {
            PatientsList.ItemsSource = _allPacientes;
        }
        else 
        {
            PatientsList.ItemsSource = _allPacientes.Where(p => 
                p.NombreCompleto.ToLower().Contains(e.NewTextValue.ToLower())
            ).ToList();
        }
    }

    // =================================================================
    // GESTIÓN DE HISTORIAL MÉDICO (MODAL DEDICADO)
    // =================================================================
    
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
                UpdateDetailUI(); // Para cambiar el color del botón a "Acceder"
                await DisplayAlert("Éxito", "Expediente clínico guardado correctamente.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo guardar el expediente.", "OK");
            }
        } 
        catch 
        { 
            await DisplayAlert("Error", "Error de conexión con el servidor.", "OK"); 
        }
    }

    // --- HERRAMIENTAS DEL EDITOR ---

    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        if (TxtHistoryEditor != null)
            TxtHistoryEditor.FontSize = e.NewValue;
    }

    private void OnFontChanged(object sender, EventArgs e)
    {
        if (TxtHistoryEditor == null || FontPicker.SelectedItem == null) return;

        var selection = FontPicker.SelectedItem as string;
        if (selection == "Monospace (Código)") 
            TxtHistoryEditor.FontFamily = "Monospace";
        else if (selection == "Serif (Formal)") 
            TxtHistoryEditor.FontFamily = "Serif";
        else 
            TxtHistoryEditor.FontFamily = "OpenSansRegular";
    }

    private void OnInsertDate(object sender, EventArgs e)
    {
        string fecha = $"\n--- {DateTime.Now:dd/MM/yyyy HH:mm} ---\n";
        TxtHistoryEditor.Text += fecha;
    }

    private void OnInsertTemplate(object sender, EventArgs e)
    {
        string template = "\nS (Subjetivo): \nO (Objetivo): \nA (Análisis): \nP (Plan): \n";
        TxtHistoryEditor.Text += template;
    }
}