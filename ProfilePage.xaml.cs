using MediSync.Helpers;
using MediSync.Models;
using System.Net.Http.Json;

namespace MediSync.Views;

public partial class ProfilePage : ContentPage
{
    private readonly HttpClient _httpClient;
    private Usuario? _currentUser;

    public ProfilePage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _httpClient = services?.GetService<HttpClient>() ?? new HttpClient { BaseAddress = new Uri("http://localhost:7151") };

        BtnCrearUsuario.IsVisible = UserInfo.IsAdminOrSuper;
    }

    private async void OnPageLoaded(object sender, EventArgs e) => await LoadUserProfile();

    private async Task LoadUserProfile()
    {
        try
        {
            var idUsuario = UserInfo.IdUsuario; 
            var response = await _httpClient.GetAsync($"api/usuarios/{idUsuario}");
            
            if (response.IsSuccessStatusCode)
            {
                _currentUser = await response.Content.ReadFromJsonAsync<Usuario>();
                if (_currentUser != null) UpdateUI();
            }
        }
        catch (Exception ex) { await DisplayAlert("Error", "No se pudo cargar el perfil.", "OK"); }
    }

    private void UpdateUI()
    {
        if (_currentUser == null) return;

        LblNombre.Text = _currentUser.NombreCompleto;
        LblRol.Text = _currentUser.Rol.ToUpper();
        LblId.Text = $"ID: {_currentUser.IdUsuario}";
        
        LblEdad.Text = $"{_currentUser.Edad} años";
        LblNacimiento.Text = _currentUser.FechaNacimiento.ToString("dd/MM/yyyy");
        LblContacto.Text = string.IsNullOrEmpty(_currentUser.Telefono) ? "--" : _currentUser.Telefono;
        LblInfo.Text = string.IsNullOrEmpty(_currentUser.Info) ? "--" : _currentUser.Info;
        
        if (!string.IsNullOrEmpty(_currentUser.Especialidad))
        {
            LblTitleEspecialidad.IsVisible = true;
            LblEspecialidad.IsVisible = true;
            LblEspecialidad.Text = _currentUser.Especialidad;
        }
        else
        {
            LblTitleEspecialidad.IsVisible = false;
            LblEspecialidad.IsVisible = false;
        }

        var parts = _currentUser.NombreCompleto.Split(' ');
        string ini = parts.Length > 0 ? parts[0][0].ToString() : "";
        if (parts.Length > 1) ini += parts[1][0].ToString();
        LblAvatar.Text = ini.ToUpper();
    }

    // --- LOGICA DE ROL ---
    private void OnRoleChanged(object sender, EventArgs e)
    {
        var rol = NewUserRol.SelectedItem?.ToString();
        if (rol == "Doctor")
        {
            // MOSTRAR BORDE Y ENTRY
            BorderEspecialidad.IsVisible = true;
            NewUserEspecialidad.IsVisible = true;
        }
        else
        {
            // OCULTAR BORDE Y ENTRY
            BorderEspecialidad.IsVisible = false;
            NewUserEspecialidad.IsVisible = false;
            NewUserEspecialidad.Text = ""; 
        }
    }

    private void OnCreateUserClicked(object sender, EventArgs e)
    {
        NewUserNombre.Text = "";
        NewUserPass.Text = "";
        NewUserContacto.Text = "";
        NewUserInfo.Text = "";
        NewUserEspecialidad.Text = "";
        NewUserRol.SelectedIndex = -1;
        
        // Reset Visibilidad
        BorderEspecialidad.IsVisible = false; 
        NewUserEspecialidad.IsVisible = false;
        
        NewUserFecha.Date = DateTime.Now.AddYears(-25);
        
        CreateUserModal.IsVisible = true;
    }

    private void CloseCreateModal(object sender, EventArgs e) => CreateUserModal.IsVisible = false;

    private async void SubmitNewUser(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewUserNombre.Text) || string.IsNullOrWhiteSpace(NewUserPass.Text) || NewUserRol.SelectedIndex == -1)
            {
                await DisplayAlert("Error", "Nombre, Contraseña y Rol son obligatorios.", "OK");
                return;
            }

            var rol = NewUserRol.SelectedItem.ToString();
            
            if (rol == "Doctor" && string.IsNullOrWhiteSpace(NewUserEspecialidad.Text))
            {
                await DisplayAlert("Atención", "Por favor indique la especialidad del Doctor.", "OK");
                return;
            }

            var newUser = new CreateUserRequest
            {
                NombreCompleto = NewUserNombre.Text,
                Password = NewUserPass.Text,
                Rol = rol,
                FechaNacimiento = NewUserFecha.Date,
                Telefono = NewUserContacto.Text,
                Info = NewUserInfo.Text,
                Especialidad = rol == "Doctor" ? NewUserEspecialidad.Text : ""
            };

            var response = await _httpClient.PostAsJsonAsync("api/usuarios/generar", newUser);
            
            if (response.IsSuccessStatusCode)
            {
                var createdUser = await response.Content.ReadFromJsonAsync<Usuario>();
                CreateUserModal.IsVisible = false;
                await DisplayAlert("Usuario Creado", $"Se ha creado el usuario con éxito.\nID Generado: {createdUser?.IdUsuario}\nRol: {createdUser?.Rol}", "OK");
            }
            else
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo crear: {errorJson}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Fallo técnico: {ex.Message}", "OK");
        }
    }

    private void OnEditProfileClicked(object sender, EventArgs e)
    {
        if (_currentUser == null) return;
        TxtEditContacto.Text = _currentUser.Telefono;
        TxtEditInfo.Text = _currentUser.Info;
        EditProfileModal.IsVisible = true;
    }

    private void CloseEditModal(object sender, EventArgs e) => EditProfileModal.IsVisible = false;

    private async void SaveProfile(object sender, EventArgs e)
    {
        if (_currentUser == null) return;
        try
        {
            var updateReq = new UpdateProfileRequest
            {
                Telefono = TxtEditContacto.Text,
                Info = TxtEditInfo.Text,
                FotoUrl = ""
            };

            var response = await _httpClient.PutAsJsonAsync($"api/usuarios/{_currentUser.IdUsuario}", updateReq);
            if (response.IsSuccessStatusCode)
            {
                _currentUser.Telefono = updateReq.Telefono;
                _currentUser.Info = updateReq.Info;
                UpdateUI();
                EditProfileModal.IsVisible = false;
                await DisplayAlert("Éxito", "Perfil actualizado.", "OK");
            }
            else await DisplayAlert("Error", "No se pudo actualizar.", "OK");
        }
        catch { await DisplayAlert("Error", "Fallo de conexión.", "OK"); }
    }
}
