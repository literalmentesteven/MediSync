using Microsoft.Extensions.Logging;
using MediSync.Views;

namespace MediSync;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Configuración de endpoint base para emulador Android (10.0.2.2) o Windows (localhost)
        string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:7151" : "http://localhost:7151";

        // Inyección del cliente HTTP con ciclo de vida Scoped
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30) // Timeout preventivo para operaciones de red
        });

        // Registro de Vistas (Transient para instanciación bajo demanda)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<PatientsPage>();
        builder.Services.AddTransient<DoctorsPage>();
        builder.Services.AddTransient<SchedulePage>();
        builder.Services.AddTransient<LaboratoryPage>();
        builder.Services.AddTransient<LabProcessingPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
