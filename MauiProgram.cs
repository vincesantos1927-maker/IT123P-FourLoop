using Plugin.Maui.Audio;
using Microsoft.Extensions.Logging;
using jeo_ano_ba.Services;
using jeo_ano_ba.Views;
using CommunityToolkit.Maui;
using jeo_ano_ba.ViewModels;
using Microsoft.Maui.Handlers;

namespace jeo_ano_ba
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // ============================================
            // API CONFIGURATION
            // ============================================
            string baseAddress = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5050/"   // android emulator 
                : "http://localhost:5050/"; // windows / iOS 

            // register the shared HttpClient 
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            });

            // ============================================
            // APP CONFIGURATION
            // ===========================================
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
            // SERVICES 
            // ============================================
            builder.Services.AddSingleton<GameDatabaseService>();
            builder.Services.AddSingleton<BgmService>();
            builder.Services.AddSingleton(AudioManager.Current);

            builder.Services.AddTransient<PlayerSetupService>();
            builder.Services.AddTransient<SavedGamesService>();
            builder.Services.AddTransient<GameTimerService>();
            builder.Services.AddSingleton<SfxService>();

            // ============================================
            // VIEW MODELS
            // ============================================
            builder.Services.AddTransient<MainMenuViewModel>();
            builder.Services.AddTransient<SavedGamesViewModel>();
            builder.Services.AddTransient<PlayerSetupViewModel>();
            builder.Services.AddTransient<NewBoardViewModel>();
            builder.Services.AddTransient<GameBoardViewModel>();
            builder.Services.AddTransient<WinnersViewModel>();

            // ============================================
            // PAGES
            // ============================================
            builder.Services.AddTransient<MainMenuPage>();
            builder.Services.AddTransient<SavedGamesPage>();
            builder.Services.AddTransient<NewBoardPage>();
            builder.Services.AddTransient<PlayerSetupPage>();
            builder.Services.AddTransient<WinnersPage>();

            // ============================================
            // GLOBAL BUTTON CLICK SOUND
            // ============================================
            var buttonPlayer = AudioManager.Current.CreatePlayer(
            FileSystem.OpenAppPackageFileAsync("click.mp3").GetAwaiter().GetResult());
            ButtonHandler.Mapper.AppendToMapping("GlobalClickSound", (handler, view) =>
            {
                if (view is Button button)
                {
                    button.Clicked += (s, e) =>
                    {
                        buttonPlayer.Seek(0);
                        buttonPlayer.Play();
                    };
                }
            });
            ImageButtonHandler.Mapper.AppendToMapping("GlobalClickSoundImg", (handler, view) =>
            {
                if (view is ImageButton imgButton)
                {
                    imgButton.Clicked += (s, e) =>
                    {
                        buttonPlayer?.Seek(0);
                        buttonPlayer?.Play();
                    };
                }
            });
            return builder.Build();
        }
    }
}