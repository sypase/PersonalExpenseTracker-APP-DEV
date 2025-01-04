using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using MauiApp2.Data.Service; // Make sure to include this namespace for UserService

namespace MauiApp2
{
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
                });

            // Add the MudBlazor services
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();

            // Register UserService as a singleton (or scoped, depending on your preference)
            builder.Services.AddSingleton<UserService>(); // This makes the service available as a singleton
            builder.Services.AddSingleton<TransactionService>();
            builder.Services.AddSingleton<DebtService>();
            builder.Services.AddSingleton<FileUploadService>();
            builder.Services.AddSingleton<FileExportService>();




#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
