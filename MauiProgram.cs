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

        string baseUrl = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:7151" : "http://localhost:7151";

        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        });

        // REGISTRO DE TODAS LAS PÁGINAS (Crucial)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<PatientsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<DoctorsPage>(); // Faltaba
        builder.Services.AddTransient<SchedulePage>(); // Faltaba
        builder.Services.AddTransient<LaboratoryPage>();
        builder.Services.AddTransient<LabProcessingPage>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}