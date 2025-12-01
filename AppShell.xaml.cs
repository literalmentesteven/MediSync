using MediSync.Views;
using MediSync.Helpers;

namespace MediSync;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        
        // Configurar accesos según el rol del usuario logueado
        ConfigureAccess();
	}

    private void ConfigureAccess()
    {
        string rol = UserInfo.Rol;

        // 1. Ocultar todo lo sensible por defecto
        TabLabDoctor.IsVisible = false;
        TabLabStaff.IsVisible = false;
        
        // 2. Lógica por Rol
        
        if (rol == "Administrador" || rol == "Superusuario")
        {
            // Admin ve Pacientes, Horarios, Doctores
            TabPacientes.IsVisible = true;
            TabHorarios.IsVisible = true;
            TabDoctores.IsVisible = true;
            
            // Admin NO ve Laboratorio (a menos que sea Super)
            if (rol == "Superusuario") 
            {
                TabLabDoctor.IsVisible = true; // Super puede ver resultados
                TabLabStaff.IsVisible = true;  // Super puede gestionar lab
            }
        }
        else if (rol == "Doctor")
        {
            // Doctor ve Pacientes, Horarios, Doctores y SU Laboratorio
            TabPacientes.IsVisible = true;
            TabHorarios.IsVisible = true;
            TabDoctores.IsVisible = true;
            TabLabDoctor.IsVisible = true; // Vista de resultados/pedidos
        }
        else if (rol == "Laboratorio")
        {
            // Personal Lab SOLO ve su gestión y su perfil
            TabPacientes.IsVisible = false;
            TabHorarios.IsVisible = false;
            TabDoctores.IsVisible = false;
            TabLabDoctor.IsVisible = false;
            TabLabStaff.IsVisible = true; // Vista de trabajo
            
            // Forzar navegación inicial a su pestaña
            this.CurrentItem = TabLabStaff;
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        UserInfo.Token = "";
        UserInfo.Rol = "";
        UserInfo.NombreUsuario = "";
        UserInfo.IdUsuario = "";
        
        var loginPage = App.Services.GetService<LoginPage>();
        if (Application.Current != null)
        {
            Application.Current.MainPage = loginPage ?? new LoginPage();
        }
    }
}
