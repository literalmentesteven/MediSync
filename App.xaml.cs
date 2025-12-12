using MediSync.Views;

namespace MediSync;

public partial class App : Application
{
    // Contenedor de servicios para resolución manual de dependencias
    public static IServiceProvider Services;

    public App(IServiceProvider provider)
    {
        InitializeComponent();
        Services = provider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Punto de entrada: Pantalla de Autenticación
        var loginPage = Services.GetService<LoginPage>();
        return new Window(loginPage ?? new LoginPage());
    }
}
