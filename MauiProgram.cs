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
            // 🛠️ DEPENDENCY INJECTION REGISTRATION
            // ====================================================================
            // 2. Registers the database service as a single persistent instance
            builder.Services.AddSingleton<GameDatabaseService>();


            // 3. Registers StartPage — this is now the app's entry point (App.xaml.cs
            //    resolves StartPage via DI, not MainPage, since MainPage needs a
            //    List<Player> that only exists after StartPage runs)
            builder.Services.AddTransient<StartPage>();

            // NOTE: MainPage is intentionally NOT registered here anymore.
            // It's constructed manually with `new MainPage(dbService, players, gameId)`
            // from inside StartPage, since its constructor needs runtime data
            // (the player list) that the DI container can't provide.
            // ====================================================================

            return builder.Build();
        }
    }
}