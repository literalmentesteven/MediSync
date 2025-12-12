using MediSync.Views;
using MediSync.Helpers;

namespace MediSync;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        ConfigureAccessControl();
	}

    /// <summary>
    /// Configura la visibilidad de los módulos según el rol del usuario (RBAC).
    /// </summary>
    private void ConfigureAccessControl()
    {
        string rol = UserInfo.Rol;

        // Principio de Mínimo Privilegio: Ocultar módulos sensibles por defecto
        TabLabDoctor.IsVisible = false;
        TabLabStaff.IsVisible = false;
        
        switch (rol)
        {
            case "Administrador":
                // Gestión operativa clínica
                TabPacientes.IsVisible = true;
                TabHorarios.IsVisible = true;
                TabDoctores.IsVisible = true;
                break;

            case "Superusuario":
                // Acceso irrestricto (Admin + Lab + Clínica)
                TabPacientes.IsVisible = true;
                TabHorarios.IsVisible = true;
                TabDoctores.IsVisible = true;
                TabLabDoctor.IsVisible = true; 
                TabLabStaff.IsVisible = true;  
                break;

            case "Doctor":
                // Acceso a pacientes y visualización de resultados
                TabPacientes.IsVisible = true;
                TabHorarios.IsVisible = true;
                TabDoctores.IsVisible = true;
                TabLabDoctor.IsVisible = true; 
                break;

            case "Laboratorio":
                // Entorno aislado de procesamiento de muestras
                TabPacientes.IsVisible = false;
                TabHorarios.IsVisible = false;
                TabDoctores.IsVisible = false;
                TabLabDoctor.IsVisible = false;
                TabLabStaff.IsVisible = true; 
                
                // Redirección forzada al módulo de trabajo
                this.CurrentItem = TabLabStaff;
                break;
        }
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        // Limpieza de sesión en memoria
        UserInfo.Token = "";
        UserInfo.Rol = "";
        UserInfo.NombreUsuario = "";
        UserInfo.IdUsuario = "";
        
        // Reinicio de la pila de navegación
        var loginPage = App.Services.GetService<LoginPage>();
        if (Application.Current != null)
        {
            Application.Current.MainPage = loginPage ?? new LoginPage();
        }
    }
}
