using Plugin.Maui.Audio;
using Microsoft.Extensions.Logging;
using jeo_ano_ba.Services;
using jeo_ano_ba.Views;
using CommunityToolkit.Maui;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // ============================================
            // PLATFORM-SPECIFIC BASE ADDRESS (Port 60449)
            // ============================================
            string baseAddress = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:60449/"   // Android emulator mapping to host loopback
                : "http://localhost:60449/"; // Windows / iOS mapping directly to localhost

            // Register single HttpClient instance using the designated address
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            });

            // Register Framework & Core Utilities
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ============================================
            // SERVICES (Clean & Single Instance)
            // ============================================
            builder.Services.AddSingleton<GameDatabaseService>();
            builder.Services.AddSingleton<BgmService>();
            builder.Services.AddSingleton(AudioManager.Current);

            builder.Services.AddTransient<PlayerSetupService>();
            builder.Services.AddTransient<SavedGamesService>();
            builder.Services.AddTransient<GameTimerService>();

            // ============================================
            // VIEW MODELS
            // ============================================
            builder.Services.AddTransient<MainMenuViewModel>();
            builder.Services.AddTransient<SavedGamesViewModel>();
            builder.Services.AddTransient<PlayerSetupViewModel>();
            builder.Services.AddTransient<NewBoardViewModel>();
            builder.Services.AddTransient<GameBoardViewModel>();

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