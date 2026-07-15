using Microsoft.Extensions.Logging;
using jeo_ano_ba.Services;
using jeo_ano_ba.Views;
using CommunityToolkit.Maui;

namespace jeo_ano_ba
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

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

            // ============================================
            // SERVICES
            // ============================================

            builder.Services.AddSingleton<GameDatabaseService>();
            builder.Services.AddSingleton<GameSessionService>();
            builder.Services.AddSingleton<GameTimerService>();
            builder.Services.AddSingleton<PlayerSetupService>();
            builder.Services.AddSingleton<CustomBoardDraftService>();
            builder.Services.AddSingleton<BgmService>();

            // ============================================
            // PAGES
            // ============================================

            builder.Services.AddTransient<MainMenuPage>();
            builder.Services.AddTransient<SavedGamesPage>();
            builder.Services.AddTransient<NewBoardPage>();
            builder.Services.AddTransient<PlayerSetupPage>();

            return builder.Build();
        }
    }
}