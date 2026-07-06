using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;

namespace jeo_ano_ba.Views;

public partial class SavedGamesPage : ContentPage
{
    private readonly GameDatabaseService _dbService;

    private bool _isLoading;

    public SavedGamesPage(GameDatabaseService dbService)
    {
        InitializeComponent();

        _dbService = dbService;
    }


    // ============================================================
    // PAGE REFRESH
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadSavedGamesAsync();
    }


    // ============================================================
    // LOAD SAVED GAMES
    // ============================================================

    private async Task LoadSavedGamesAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;

            GamesContainer.Children.Clear();

            List<GameDb> allGames =
                await _dbService.GetAllGamesAsync();

            // Do not show Master Library / preset games
            List<GameDb> savedGames = allGames
                .Where(game => !game.IsPreset)
                .OrderByDescending(game => game.Id)
                .ToList();


            // EMPTY STATE

            if (savedGames.Count == 0)
            {
                EmptyState.IsVisible = true;

                GamesScrollView.IsVisible = false;

                return;
            }


            // SAVED GAMES EXIST

            EmptyState.IsVisible = false;

            GamesScrollView.IsVisible = true;


            foreach (GameDb game in savedGames)
            {
                Border gameCard =
                    await CreateGameCardAsync(game);

                GamesContainer.Children.Add(gameCard);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Load Failed",
                ex.Message,
                "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }


    // ============================================================
    // CREATE SAVED GAME CARD
    // ============================================================

    private async Task<Border> CreateGameCardAsync(GameDb game)
    {
        int categoryCount = 0;
        int clueCount = 0;

        List<string> categoryNames = new();

        try
        {
            GameDb detailedGame =
                await _dbService.GetGameWithDetailsAsync(game.Id);

            if (detailedGame.Categories != null)
            {
                categoryCount = detailedGame.Categories.Count;

                clueCount = detailedGame.Categories.Sum(category =>
                    category.Clues?.Count ?? 0);

                categoryNames = detailedGame.Categories
                    .Select(category => category.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
            }
        }
        catch
        {
            // Card can still display if details fail to load.
        }


        // GAME TITLE

        Label titleLabel = new()
        {
            Text = game.Name,

            FontSize = 21,

            FontAttributes =
                FontAttributes.Bold,

            TextColor =
                Colors.White,

            LineBreakMode =
                LineBreakMode.TailTruncation
        };


        // GAME INFO

        Label dateLabel = new()
        {
            // Temporary until we add/use a CreatedAt property in GameDb.
            Text = "Date created unavailable",

            FontSize = 14,

            TextColor = Color.FromArgb("#D7B53C")
        };


        string categoryPreview = categoryNames.Count > 0
            ? string.Join(" · ", categoryNames)
            : "No categories";

        Label categoriesLabel = new()
        {
            Text = categoryPreview,

            FontSize = 14,

            TextColor = Color.FromArgb("#6E82A1"),

            LineBreakMode = LineBreakMode.TailTruncation,

            MaxLines = 1
        };


        // EDIT BUTTON

        Label editLabel = new()
        {
            Text = "EDIT BOARD",

            FontSize = 13,

            FontAttributes =
                FontAttributes.Bold,

            TextColor =
                Color.FromArgb("#FFD700"),

            VerticalOptions =
                LayoutOptions.Center
        };


        // PLAY BUTTON

        Label playLabel = new()
        {
            Text = "PLAY  ›",

            FontSize = 15,

            FontAttributes =
                FontAttributes.Bold,

            TextColor =
                Color.FromArgb("#FFD700"),

            HorizontalOptions =
                LayoutOptions.End,

            VerticalOptions =
                LayoutOptions.Center
        };


        // DELETE BUTTON

        Label deleteLabel = new()
        {
            Text = "DELETE BOARD",

            FontSize = 13,

            FontAttributes = FontAttributes.Bold,

            TextColor = Color.FromArgb("#ff4d4d"),

            VerticalOptions = LayoutOptions.Center
        };


        // ==========================
        // EDIT TAP
        // ==========================

        TapGestureRecognizer editTap = new();

        editTap.Tapped += async (_, _) =>
        {
            await Navigation.PushAsync(
                new NewBoardPage(
                    _dbService,
                    game.Id));
        };

        editLabel.GestureRecognizers.Add(editTap);


        // ==========================
        // PLAY TAP
        // ==========================

        TapGestureRecognizer playTap = new();

        playTap.Tapped += async (_, _) =>
        {
            await Navigation.PushAsync(
                new PlayerSetupPage(
                    _dbService,
                    game.Id));
        };

        playLabel.GestureRecognizers.Add(playTap);


        // ==========================
        // DELETE TAP
        // ==========================

        TapGestureRecognizer deleteTap = new();

        deleteTap.Tapped += async (_, _) =>
        {
            bool confirmed =
                await DisplayAlertAsync(
                    "Delete Game",
                    $"Delete \"{game.Name}\"?",
                    "Delete",
                    "Cancel");

            if (!confirmed)
                return;


            try
            {
                await _dbService.DeleteGameAsync(game.Id);

                await LoadSavedGamesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Delete Failed",
                    ex.Message,
                    "OK");
            }
        };

        deleteLabel.GestureRecognizers.Add(deleteTap);


        // ==========================
        // ACTION ROW
        // ==========================

        // Left side contains EDIT BOARD + DELETE

        HorizontalStackLayout leftActions = new()
        {
            Spacing = 22,

            VerticalOptions = LayoutOptions.Center,

            Children =
    {
        editLabel,
        deleteLabel
    }
        };


        Grid actionGrid = new()
        {
            ColumnDefinitions =
    {
        new ColumnDefinition(GridLength.Star),

        new ColumnDefinition(GridLength.Auto)
    }
        };


        // LEFT SIDE

        actionGrid.Add(
            leftActions,
            0,
            0);


        // RIGHT SIDE

        actionGrid.Add(
            playLabel,
            1,
            0);


        // ==========================
        // CARD CONTENT
        // ==========================

        VerticalStackLayout cardContent = new()
        {
            Spacing = 7,

            Children =
    {
        titleLabel,

        dateLabel,

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


        // ==========================
        // CARD
        // ==========================

        Border card = new()
        {
            Padding =
                new Thickness(
                    18,
                    16),

            BackgroundColor =
                Color.FromArgb("#102646"),

            Stroke =
                Color.FromArgb("#31547F"),

            StrokeThickness = 1,

            StrokeShape =
                new RoundRectangle
                {
                    CornerRadius =
                        new CornerRadius(18)
                },

            Content =
                cardContent
        };


        return card;
    }


    // ============================================================
    // CLOSE PAGE
    // ============================================================

    private async void CloseTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }


    // ============================================================
    // CREATE NEW BOARD
    // ============================================================

    private async void CreateTapped(
        object sender,
        TappedEventArgs e)
    {
        await CreateButton.ScaleToAsync(
            0.90,
            70,
            Easing.CubicIn);

        await CreateButton.ScaleToAsync(
            1.00,
            120,
            Easing.SpringOut);


        await Navigation.PushAsync(
            new NewBoardPage(_dbService));
    }
}