using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace jeo_ano_ba.Views;

public partial class NewBoardPage : ContentPage
{
    private readonly GameDatabaseService _dbService;

    // null = creating a new board
    // has value = editing an existing saved board
    private readonly int? _editingGameId;

    private readonly int[] values = { 200, 400, 600, 800, 1000 };

    private Border? _selectedTile;

    private int _selectedRow;
    private int _selectedColumn;

    // =============================
    // BOARD DATA
    // =============================

    private readonly Entry[] _categoryEntries = new Entry[6];

    private readonly string[,] _questions = new string[6, 5];
    private readonly string[,] _answers = new string[6, 5];

    private readonly bool[,] _filled = new bool[6, 5];

    private readonly Label[,] _cellLabels = new Label[6, 5];


    // ============================================================
    // CREATE MODE CONSTRUCTOR
    // ============================================================

    public NewBoardPage(GameDatabaseService dbService)
    {
        InitializeComponent();

        _dbService = dbService;
        _editingGameId = null;

        BuildBoard();
    }


    // ============================================================
    // EDIT MODE CONSTRUCTOR
    // ============================================================

    public NewBoardPage(
        GameDatabaseService dbService,
        int gameId)
    {
        InitializeComponent();

        _dbService = dbService;
        _editingGameId = gameId;

        BuildBoard();

        Loaded += NewBoardPageLoaded;
    }


    // ============================================================
    // BUILD BOARD UI
    // ============================================================

    private void BuildBoard()
    {
        BoardGrid.Children.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.RowDefinitions.Clear();

        // Same spacing as MainPage
        BoardGrid.ColumnSpacing = 4;
        BoardGrid.RowSpacing = 4;

        // 6 equal columns
        for (int i = 0; i < 6; i++)
        {
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Star
            });
        }

        // Category row
        BoardGrid.RowDefinitions.Add(new RowDefinition
        {
            Height = 58
        });

        // Five clue rows
        for (int i = 0; i < 5; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Star
            });
        }

        // ===============================
        // CATEGORY HEADERS
        // ===============================

        for (int col = 0; col < 6; col++)
        {
            var entry = new Entry
            {
                Placeholder = "Category",
                PlaceholderColor = Color.FromArgb("#7E8DA9"),
                TextColor = Color.FromArgb("#FFD700"),

                BackgroundColor = Colors.Transparent,

                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,

                FontSize = 10,
                FontAttributes = FontAttributes.Bold,

                Margin = 0
            };

            _categoryEntries[col] = entry;

            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#1E1E3F"),

                Stroke = Color.FromArgb("#FFD700"),
                StrokeThickness = 1,

                Padding = 4,

                HeightRequest = 54,

                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(10)
                },

                Content = entry
            };

            Grid.SetRow(border, 0);
            Grid.SetColumn(border, col);

            BoardGrid.Children.Add(border);
        }

        // ===============================
        // CLUE CELLS
        // ===============================

        for (int row = 1; row <= 5; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                int tileRow = row - 1;
                int tileColumn = col;
                int tileValue = values[tileRow];

                var label = new Label
                {
                    Text = $"${tileValue}",

                    TextColor = Color.FromArgb("#FFD700"),

                    FontSize = 16,

                    FontAttributes = FontAttributes.Bold,

                    HorizontalOptions = LayoutOptions.Center,

                    VerticalOptions = LayoutOptions.Center,

                    HorizontalTextAlignment = TextAlignment.Center,

                    VerticalTextAlignment = TextAlignment.Center
                };

                _cellLabels[tileColumn, tileRow] = label;

                var tile = new Border
                {
                    BackgroundColor = Color.FromArgb("#284C84"),

                    Stroke = Color.FromArgb("#375D99"),

                    StrokeThickness = 1,

                    Padding = 2,

                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = new CornerRadius(10)
                    },

                    Content = label
                };

                var tap = new TapGestureRecognizer();

                tap.Tapped += (sender, args) =>
                {
                    if (_selectedTile != null)
                    {
                        _selectedTile.StrokeThickness = 1;
                        _selectedTile.Stroke = Color.FromArgb("#FFCC00");
                    }

                    _selectedRow = tileRow;
                    _selectedColumn = tileColumn;

                    _selectedTile = tile;

                    _selectedTile.StrokeThickness = 3;
                    _selectedTile.Stroke = Colors.White;

                    PopupValueLabel.Text = $"${tileValue}";

                    QuestionEditor.Text =
                        _questions[tileColumn, tileRow] ?? "";

                    AnswerEntry.Text =
                        _answers[tileColumn, tileRow] ?? "";

                    PopupOverlay.IsVisible = true;
                };

                tile.GestureRecognizers.Add(tap);

                Grid.SetRow(tile, row);
                Grid.SetColumn(tile, col);

                BoardGrid.Children.Add(tile);
            }
        }
    }


    // ============================================================
    // LOAD EXISTING SAVED BOARD
    // ============================================================

    private async void NewBoardPageLoaded(
        object? sender,
        EventArgs e)
    {
        Loaded -= NewBoardPageLoaded;

        await LoadExistingBoardAsync();
    }


    private async Task LoadExistingBoardAsync()
    {
        if (!_editingGameId.HasValue)
            return;


        try
        {
            GameDb game =
                await _dbService.GetGameWithDetailsAsync(
                    _editingGameId.Value);


            List<CategoryDb> categories =
                game.Categories
                    .OrderBy(category => category.Id)
                    .ToList();


            if (categories.Count != 6)
            {
                await DisplayAlertAsync(
                    "Cannot Edit Board",
                    "This saved game does not contain exactly 6 categories.",
                    "OK");

                await Navigation.PopAsync();

                return;
            }


            for (int column = 0;
                 column < 6;
                 column++)
            {
                CategoryDb category =
                    categories[column];


                // Load category name

                _categoryEntries[column].Text =
                    category.Name;


                List<ClueDb> clues =
                    category.Clues
                        .OrderBy(clue => clue.PointValue)
                        .ToList();


                if (clues.Count != 5)
                {
                    await DisplayAlertAsync(
                        "Cannot Edit Board",
                        $"Category {column + 1} does not contain exactly 5 clues.",
                        "OK");

                    await Navigation.PopAsync();

                    return;
                }


                for (int row = 0;
                     row < 5;
                     row++)
                {
                    ClueDb clue =
                        clues[row];


                    _questions[column, row] =
                        clue.Question;


                    _answers[column, row] =
                        clue.Answer;


                    _filled[column, row] =
                        true;


                    Label cellLabel =
                        _cellLabels[column, row];


                    cellLabel.Text =
                        "✓";


                    cellLabel.TextColor =
                        Color.FromArgb("#FFD700");


                    cellLabel.FontSize =
                        24;
                }
            }


            UpdateProgress();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Load Failed",
                ex.Message,
                "OK");
        }
    }


    // ============================================================
    // CLOSE POPUP
    // ============================================================

    private void ClosePopupTapped(
        object sender,
        TappedEventArgs e)
    {
        PopupOverlay.IsVisible =
            false;


        if (_selectedTile != null)
        {
            _selectedTile.StrokeThickness =
                1;

            _selectedTile.Stroke =
                Color.FromArgb("#375D99");

            _selectedTile =
                null;
        }
    }


    // ============================================================
    // SAVE CLUE IN MEMORY
    // ============================================================

    private async void SaveQuestionTapped(
        object sender,
        TappedEventArgs e)
    {
        string question =
            QuestionEditor.Text?.Trim()
            ?? string.Empty;


        string answer =
            AnswerEntry.Text?.Trim()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(question) ||
            string.IsNullOrWhiteSpace(answer))
        {
            await DisplayAlertAsync(
                "Missing Information",
                "Please enter both the question and the correct answer.",
                "OK");

            return;
        }


        _questions[
            _selectedColumn,
            _selectedRow] =
            question;


        _answers[
            _selectedColumn,
            _selectedRow] =
            answer;


        _filled[
            _selectedColumn,
            _selectedRow] =
            true;


        Label selectedLabel =
            _cellLabels[
                _selectedColumn,
                _selectedRow];


        selectedLabel.Text =
            "✓";


        selectedLabel.TextColor =
            Color.FromArgb("#FFD700");


        selectedLabel.FontSize =
            24;


        PopupOverlay.IsVisible =
            false;


        if (_selectedTile != null)
        {
            _selectedTile.StrokeThickness =
                1;

            _selectedTile.Stroke =
                Color.FromArgb("#375D99");

            _selectedTile =
                null;
        }


        UpdateProgress();
    }


    // ============================================================
    // UPDATE PROGRESS
    // ============================================================

    private void UpdateProgress()
    {
        int filledCount = 0;


        for (int column = 0;
             column < 6;
             column++)
        {
            for (int row = 0;
                 row < 5;
                 row++)
            {
                if (_filled[column, row])
                {
                    filledCount++;
                }
            }
        }


        ProgressLabel.Text =
            $"{filledCount}/30 filled";
    }


    // ============================================================
    // BUILD CATEGORY DATA FROM UI
    // ============================================================

    private async Task<List<CustomCategoryInput>?>
        BuildCategoriesFromBoardAsync()
    {
        var categories =
            new List<CustomCategoryInput>();


        for (int column = 0;
             column < 6;
             column++)
        {
            string categoryName =
                _categoryEntries[column]
                    .Text?
                    .Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(
                categoryName))
            {
                await DisplayAlertAsync(
                    "Missing Information",
                    "Please enter all 6 category names.",
                    "OK");

                return null;
            }


            var clues =
                new List<(string, string)>();


            for (int row = 0;
                 row < 5;
                 row++)
            {
                if (!_filled[column, row])
                {
                    await DisplayAlertAsync(
                        "Incomplete Board",
                        "Please complete all 30 questions and answers before saving.",
                        "OK");

                    return null;
                }


                clues.Add((
                    _questions[column, row],
                    _answers[column, row]
                ));
            }


            categories.Add(
                new CustomCategoryInput
                {
                    CategoryName =
                        categoryName,

                    Clues =
                        clues
                });
        }


        return categories;
    }


    // ============================================================
    // SAVE BOARD
    // ============================================================

    private async Task<int?> SaveBoardAsync()
    {
        List<CustomCategoryInput>? categories =
            await BuildCategoriesFromBoardAsync();


        if (categories == null)
            return null;


        try
        {
            // ====================================================
            // EDIT MODE
            // ====================================================

            if (_editingGameId.HasValue)
            {
                await _dbService.UpdatePlayerAuthoredGameAsync(
                    _editingGameId.Value,
                    "Custom Game",
                    categories,
                    startingPointValue: 100,
                    pointIncrement: 100);


                return _editingGameId.Value;
            }


            // ====================================================
            // CREATE MODE
            // ====================================================

            int gameId =
                await _dbService.BuildPlayerAuthoredGameAsync(
                    "Custom Game",
                    categories,
                    startingPointValue: 100,
                    pointIncrement: 100);


            return gameId;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Save Failed",
                ex.Message,
                "OK");


            return null;
        }
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
    // SAVE ONLY
    // ============================================================

    private async void SaveOnlyTapped(
        object sender,
        TappedEventArgs e)
    {
        int? gameId =
            await SaveBoardAsync();


        if (gameId == null)
            return;


        string message =
            _editingGameId.HasValue
                ? "Your saved board was updated successfully."
                : "Your custom board was saved successfully.";


        await DisplayAlertAsync(
            "Saved",
            message,
            "OK");


        await Navigation.PopAsync();
    }


    // ============================================================
    // SAVE & START
    // ============================================================

    private async void SaveStartTapped(
        object sender,
        TappedEventArgs e)
    {
        int? gameId =
            await SaveBoardAsync();


        if (gameId == null)
            return;


        await Navigation.PushAsync(
            new PlayerSetupPage(
                _dbService,
                gameId.Value));
    }
}