using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Services;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Views;

public partial class MainMenuPage : ContentPage {
    private readonly GameDatabaseService _dbService;
    private readonly MainMenuViewModel _viewModel;
    private readonly SfxService _sfxService;
    // Services/ViewModel are injected via DI
    public MainMenuPage(
    GameDatabaseService dbService,
    MainMenuViewModel viewModel, 
    SfxService sfxService)
    {
        InitializeComponent();
        _dbService = dbService;
        _viewModel = viewModel;
        _sfxService = sfxService;
        BindingContext = _viewModel;

        // Listen for ViewModel changes (e.g. music toggled elsewhere)
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateMusicButton();
    }

    // Runs every time this page becomes visible, not just on first load
    protected override void OnAppearing() {
        base.OnAppearing();
        // Re-sync in case music state changed on another page
        _viewModel.RefreshMusicState();
    }

    // Reacts only to the IsMusicEnabled property changing
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(MainMenuViewModel.IsMusicEnabled))
            UpdateMusicButton();
    }

    // Updates icon/color to match current music state
    private void UpdateMusicButton() {
        if (_viewModel.IsMusicEnabled) {
            MusicIcon.Text = "🔊";
            MusicButton.BackgroundColor = Color.FromArgb("#2F5D95");
        }
        else {
            MusicIcon.Text = "🔇";
            MusicButton.BackgroundColor = Color.FromArgb("#234A78");
        }
    }

    // Shrink-then-bounce press feedback, shared by menu buttons
    private async Task AnimateButton(Border button) {
        await button.ScaleToAsync(0.96, 80, Easing.CubicIn);
        await button.ScaleToAsync(1.02, 120, Easing.CubicOut);
        await button.ScaleToAsync(1.00, 80, Easing.SpringOut);
    }

    // "Custom Game" tapped — animate, then go to saved games list
    private async void CustomGameTapped(object sender, TappedEventArgs e) {
        _sfxService.PlayClick();
        await AnimateButton(CustomGameButton);
        await Navigation.PushAsync(
            new SavedGamesPage(_dbService, _sfxService));
    }

    // "Preset Categories" tapped — pick categories, build the game, then move to player setup
    private async void GeneralKnowledgeTapped(object sender, TappedEventArgs e) {
        _sfxService.PlayClick();
        await AnimateButton(GeneralKnowledgeButton);

        var categories = await _viewModel.GetAvailableCategoriesAsync();
        if (categories.Count == 0) {
            await DisplayAlert(
                "No Categories Available",
                "There are no categories available to select. Please add categories first.",
                "OK");
            return;
        }

        // Popup returns null if cancelled, or the list of chosen categories
        var popup = new CategorySelectorPopup(categories);
        var result = await this.ShowPopupAsync(popup);

        // Bail out unless exactly 6 categories were chosen
        if (result is not List<string> chosenCategories || chosenCategories.Count != 6)
            return;

        int gameId = await _viewModel.BuildGeneralKnowledgeGameAsync(chosenCategories);
        await Navigation.PushAsync(new PlayerSetupPage(_dbService, _sfxService, new PlayerSetupViewModel(_dbService, new PlayerSetupService()), gameId));
    }

    // Music icon tapped — bounce animation, then flip music on/off
    private async void MusicTapped(object sender, TappedEventArgs e) {
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