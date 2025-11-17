namespace MediSync; // (O el nombre de tu app, ej: MiAppMaui)

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // ESTA ES LA LÍNEA QUE CAMBIAS
        // Antes decía: return new Window(new AppShell());
        return new Window(new LoginPage());
    }
}