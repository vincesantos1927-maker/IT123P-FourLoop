using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.Views;

public partial class MainMenuPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private bool _musicEnabled = true;

    public MainMenuPage(GameDatabaseService dbService)
    {
        InitializeComponent();

        _dbService = dbService;
    }

    private async Task AnimateButton(Border button)
    {
        await button.ScaleToAsync(0.96, 80, Easing.CubicIn);
        await button.ScaleToAsync(1.02, 120, Easing.CubicOut);
        await button.ScaleToAsync(1.00, 80, Easing.SpringOut);
    }

    private async void CustomGameTapped(object sender, TappedEventArgs e)
    {
        await AnimateButton(CustomGameButton);

        await Navigation.PushAsync(
            new SavedGamesPage(_dbService));
    }

    private async void GeneralKnowledgeTapped(object sender, TappedEventArgs e)
    {
        await AnimateButton(GeneralKnowledgeButton);

        var categories = await _dbService.GetAvailableCategoriesAsync();

        if (categories == null || categories.Count == 0)
        {
            await DisplayAlert(
                "No Categories Found",
                "There are no preset categories available yet.",
                "OK");
            return;
        }

        var popup = new CategorySelectorPopup(categories);
        var result = await this.ShowPopupAsync(popup);

        // result is null if the user cancelled
        if (result is not List<string> chosenCategories || chosenCategories.Count != 6)
            return;

        int gameId = await _dbService.BuildCustomGameFromCategoriesAsync(
            "General Knowledge",
            chosenCategories);

        await Navigation.PushAsync(new PlayerSetupPage(_dbService, gameId));
    }

    private async void MusicTapped(object sender, TappedEventArgs e)
    {
        await MusicButton.ScaleToAsync(
            0.92,
            80,
            Easing.CubicIn);

        await MusicButton.ScaleToAsync(
            1.0,
            120,
            Easing.SpringOut);

        _musicEnabled = !_musicEnabled;

        if (_musicEnabled)
        {
            MusicIcon.Text = "🔊";
            MusicButton.BackgroundColor =
                Color.FromArgb("#2F5D95");
        }
        else
        {
            MusicIcon.Text = "🔇";
            MusicButton.BackgroundColor =
                Color.FromArgb("#234A78");
        }
    }
}