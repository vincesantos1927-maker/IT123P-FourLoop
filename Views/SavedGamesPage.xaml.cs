using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Views;

public partial class SavedGamesPage : ContentPage {
    private readonly GameDatabaseService _dbService;
    private readonly SfxService _sfxService;
    private readonly SavedGamesViewModel _viewModel;

    private bool _isLoading;

    public SavedGamesPage(GameDatabaseService dbService, SfxService sfxService) {
        InitializeComponent();

        _dbService = dbService;
        _sfxService = sfxService;
        _viewModel = new SavedGamesViewModel(new SavedGamesService(dbService));

        BindingContext = _viewModel;
    }

    // Refresh the list every time this page becomes visible (e.g. after creating/editing a board)
    protected override async void OnAppearing() {
        base.OnAppearing();
        await LoadSavedGamesAsync();
    }

    // Fetches saved games and rebuilds the list UI, or shows the empty state if none exist
    private async Task LoadSavedGamesAsync() {
        if (_isLoading)
            return;

        try {
            _isLoading = true;

            GamesContainer.Children.Clear();

            List<SavedGameCardViewModel> savedGames = await _viewModel.LoadSavedGameCardsAsync();

            if (savedGames.Count == 0) {
                EmptyState.IsVisible = true;
                GamesScrollView.IsVisible = false;
                return;
            }

            EmptyState.IsVisible = false;
            GamesScrollView.IsVisible = true;

            foreach (SavedGameCardViewModel game in savedGames) {
                Border gameCard = await CreateGameCardAsync(game);
                GamesContainer.Children.Add(gameCard);
            }
        }
        catch (Exception ex) {
            await DisplayAlertAsync(
                "Load Failed",
                ex.Message,
                "OK");
        }
        finally {
            _isLoading = false;
        }
    }

    // Builds a single saved-game card: title, category preview, and Edit/Delete/Play actions
    private Task<Border> CreateGameCardAsync(SavedGameCardViewModel game) {
        Label titleLabel = new() {
            Text = game.Name,
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        string categoryPreview = game.CategoryPreview;

        Label categoriesLabel = new() {
            Text = categoryPreview,
            FontSize = 14,
            TextColor = Color.FromArgb("#6E82A1"),
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

        Label editLabel = new() {
            Text = "EDIT BOARD",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD700"),
            VerticalOptions = LayoutOptions.Center
        };

        Label playLabel = new() {
            Text = "PLAY  ›",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD700"),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        Label deleteLabel = new() {
            Text = "DELETE BOARD",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#ff4d4d"),
            VerticalOptions = LayoutOptions.Center
        };

        // Edit → go to NewBoardPage in edit mode for this game
        TapGestureRecognizer editTap = new();
        editTap.Tapped += async (_, _) => {
            _sfxService.PlayClick();
            await Navigation.PushAsync(
                new NewBoardPage(
                    _dbService,
                    _sfxService,
                    game.Id));
        };
        editLabel.GestureRecognizers.Add(editTap);

        // Play → go straight to player setup for this game
        TapGestureRecognizer playTap = new();
        playTap.Tapped += async (_, _) => {
            _sfxService.PlayClick();
            await Navigation.PushAsync(
                new PlayerSetupPage(
                    _dbService,
                    _sfxService,
                    new PlayerSetupViewModel(_dbService, new PlayerSetupService()),
                    game.Id));
        };
        playLabel.GestureRecognizers.Add(playTap);

        // Delete → confirm, then remove from DB and refresh the list
        TapGestureRecognizer deleteTap = new();
        deleteTap.Tapped += async (_, _) => {
            _sfxService.PlayClick();

            bool confirmed =
                await DisplayAlertAsync(
                    "Delete Game",
                    $"Delete \"{game.Name}\"?",
                    "Delete",
                    "Cancel");

            if (!confirmed)
                return;

            try {
                await _viewModel.DeleteGameAsync(game.Id);
                await LoadSavedGamesAsync();
            }
            catch (Exception ex) {
                await DisplayAlertAsync(
                    "Delete Failed",
                    ex.Message,
                    "OK");
            }
        };
        deleteLabel.GestureRecognizers.Add(deleteTap);

        // Action row: Edit + Delete on the left, Play on the right
        HorizontalStackLayout leftActions = new() {
            Spacing = 22,
            VerticalOptions = LayoutOptions.Center,
            Children =
    {
        editLabel,
        deleteLabel
    }
        };

        Grid actionGrid = new() {
            ColumnDefinitions =
    {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(GridLength.Auto)
    }
        };

        actionGrid.Add(leftActions, 0, 0);
        actionGrid.Add(playLabel, 1, 0);

        VerticalStackLayout cardContent = new() {
            Spacing = 7,
            Children =
    {
        titleLabel,
        categoriesLabel,
        new BoxView
        {
            HeightRequest = 1,
            Margin = new Thickness(0, 8),
            BackgroundColor = Color.FromArgb("#29476E")
        },
        actionGrid
    }
        };

        Border card = new() {
            Padding = new Thickness(18, 16),
            BackgroundColor = Color.FromArgb("#102646"),
            Stroke = Color.FromArgb("#31547F"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle {
                CornerRadius = new CornerRadius(18)
            },
            Content = cardContent
        };

        return Task.FromResult(card);
    }

    private async void CloseTapped(
        object sender,
        TappedEventArgs e) {
        _sfxService.PlayClick();
        await Navigation.PopAsync();
    }

    // "+" tapped — bounce animation, then go create a new board
    private async void CreateTapped(
        object sender,
        TappedEventArgs e) {
        _sfxService.PlayClick();
        await CreateButton.ScaleToAsync(
            0.90,
            70,
            Easing.CubicIn);

        await CreateButton.ScaleToAsync(
            1.00,
            120,
            Easing.SpringOut);

        await Navigation.PushAsync(
            new NewBoardPage(_dbService, _sfxService));
    }
}