using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SmartJobMobile.Services;

namespace SmartJobMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder =
                MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services
                .AddSingleton<
                    ConnectivityService>();

            builder.Services
                .AddSingleton<
                    LocalDatabaseService>();

            builder.Services
                .AddSingleton<
                    ITokenStorageService,
                    TokenStorageService>();

            builder.Services
                .AddSingleton<
                    SessionService>();

            builder.Services
                .AddSingleton<
                    AuthenticationService>();

            builder.Services
                .AddSingleton<
                    ApiService>();

            return builder.Build();
        }
    }
}