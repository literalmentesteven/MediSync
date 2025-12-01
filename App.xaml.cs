using MediSync.Views;

namespace MediSync;

public partial class App : Application
{
    // Proveedor de servicios para resolver dependencias manualmente si es necesario en el arranque
    public static IServiceProvider Services;

    public App(IServiceProvider provider)
    {
        InitializeComponent();
        Services = provider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Iniciamos siempre en el Login
        var loginPage = Services.GetService<LoginPage>();
        return new Window(loginPage ?? new LoginPage());
    }
}
