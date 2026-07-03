using Microsoft.Extensions.Logging;
using jeo_ano_ba.Services; // Ensures it can find your database service
using CommunityToolkit.Maui; // 🛠️ Required for UseMauiCommunityToolkit()

namespace jeo_ano_ba
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // 🛠️ 1. Initializes the Popup/Toolkit Framework
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ====================================================================
            // 🛠️ DEPENDENCY INJECTION REGISTRATION (CRITICAL FIX FOR THE CRASH)
            // ====================================================================

            // 2. Registers the database service as a single persistent instance
            builder.Services.AddSingleton<GameDatabaseService>();

            // 3. Registers your MainPage so the framework knows how to pass the service into its constructor
            builder.Services.AddTransient<MainPage>();

            // ====================================================================

            return builder.Build();
        }
    }
}