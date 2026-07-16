using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Services;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Views;

public partial class MainMenuPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private readonly MainMenuViewModel _viewModel;

    public MainMenuPage(
    GameDatabaseService dbService,
    MainMenuViewModel viewModel)
    {
        InitializeComponent();

        _dbService = dbService;
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateMusicButton();
    }
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainMenuViewModel.IsMusicEnabled))
            UpdateMusicButton();
    }
    private void UpdateMusicButton()
    {
        if (_viewModel.IsMusicEnabled)
        {
            MusicIcon.Text = "🔊";
            MusicButton.BackgroundColor = Color.FromArgb("#2F5D95");
        }
        else
        {
            MusicIcon.Text = "🔇";
            MusicButton.BackgroundColor = Color.FromArgb("#234A78");
        }
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

        var categories = await _viewModel.GetAvailableCategoriesAsync();

        if (categories.Count == 0)
        {
            await DisplayAlert(
                "No Categories Available",
                "There are no categories available to select. Please add categories first.",
                "OK");
            return;
        }

        var popup = new CategorySelectorPopup(categories);
        var result = await this.ShowPopupAsync(popup);

        // result is null if the user cancelled
        if (result is not List<string> chosenCategories || chosenCategories.Count != 6)
            return;

        int gameId = await _viewModel.BuildGeneralKnowledgeGameAsync(chosenCategories);

        await Navigation.PushAsync(new PlayerSetupPage(_dbService, new PlayerSetupViewModel(_dbService, new PlayerSetupService()), gameId));
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

        _viewModel.ToggleMusic();
    }
}