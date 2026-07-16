using jeo_ano_ba.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Views;

public partial class NewBoardPage : ContentPage {
    private readonly GameDatabaseService _dbService;

    // Null = creating a new board, has value = editing an existing saved board
    private readonly int? _editingGameId;
    private readonly NewBoardViewModel _viewModel;

    private readonly int[] values = { 200, 400, 600, 800, 1000 };

    private Border? _selectedTile;
    private int _selectedRow;
    private int _selectedColumn;

    // Board data — one entry per category column, one label per clue cell
    private readonly Entry[] _categoryEntries = new Entry[6];
    private readonly Label[,] _cellLabels = new Label[6, 5];

    // Create mode: fresh empty board
    public NewBoardPage(GameDatabaseService dbService) {
        InitializeComponent();

        _dbService = dbService;
        _editingGameId = null;
        _viewModel = new NewBoardViewModel(_dbService, _editingGameId);
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BuildBoard();
    }

    // Edit mode: loads an existing saved board after the page appears
    public NewBoardPage(
    GameDatabaseService dbService,
    int gameId) {
        InitializeComponent();

        _dbService = dbService;
        _editingGameId = gameId;
        _viewModel = new NewBoardViewModel(_dbService, _editingGameId);
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        BuildBoard();

        Loaded += NewBoardPageLoaded;
    }

    // Keeps the "X/30 filled" counter in sync with the ViewModel
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(NewBoardViewModel.ProgressText))
            ProgressLabel.Text = _viewModel.ProgressText;
    }

    // Generates the 6x5 board (category row + 5 clue rows) entirely in code
    private void BuildBoard() {
        BoardGrid.Children.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.RowDefinitions.Clear();

        BoardGrid.ColumnSpacing = 4;
        BoardGrid.RowSpacing = 4;

        // 6 equal-width columns
        for (int i = 0; i < 6; i++) {
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition {
                Width = GridLength.Star
            });
        }

        // Category header row (fixed height)
        BoardGrid.RowDefinitions.Add(new RowDefinition {
            Height = 58
        });

        // 5 clue rows (equal height, fill remaining space)
        for (int i = 0; i < 5; i++) {
            BoardGrid.RowDefinitions.Add(new RowDefinition {
                Height = GridLength.Star
            });
        }

        // Category header entries (row 0) — one editable text box per column
        for (int col = 0; col < 6; col++) {
            var entry = new Entry {
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

            var border = new Border {
                BackgroundColor = Color.FromArgb("#1E1E3F"),
                Stroke = Color.FromArgb("#FFD700"),
                StrokeThickness = 1,
                Padding = 4,
                HeightRequest = 54,
                StrokeShape = new RoundRectangle {
                    CornerRadius = new CornerRadius(10)
                },
                Content = entry
            };

            Grid.SetRow(border, 0);
            Grid.SetColumn(border, col);

            BoardGrid.Children.Add(border);
        }

        // Clue cells (rows 1-5) — each tile shows its dollar value until filled,
        // and opens the question/answer popup when tapped
        for (int row = 1; row <= 5; row++) {
            for (int col = 0; col < 6; col++) {
                int tileRow = row - 1;
                int tileColumn = col;
                int tileValue = values[tileRow];

                var label = new Label {
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

                var tile = new Border {
                    BackgroundColor = Color.FromArgb("#284C84"),
                    Stroke = Color.FromArgb("#375D99"),
                    StrokeThickness = 1,
                    Padding = 2,
                    StrokeShape = new RoundRectangle {
                        CornerRadius = new CornerRadius(10)
                    },
                    Content = label
                };

                var tap = new TapGestureRecognizer();

                tap.Tapped += (sender, args) => {
                    // Deselect the previously selected tile (reset its border)
                    if (_selectedTile != null) {
                        _selectedTile.StrokeThickness = 1;
                        _selectedTile.Stroke = Color.FromArgb("#FFCC00");
                    }

                    _selectedRow = tileRow;
                    _selectedColumn = tileColumn;
                    _selectedTile = tile;

                    // Highlight the newly selected tile
                    _selectedTile.StrokeThickness = 3;
                    _selectedTile.Stroke = Colors.White;

                    // Pre-fill popup with any existing question/answer for this cell
                    PopupValueLabel.Text = $"${tileValue}";
                    QuestionEditor.Text = _viewModel.GetQuestion(tileColumn, tileRow);
                    AnswerEntry.Text = _viewModel.GetAnswer(tileColumn, tileRow);

                    PopupOverlay.IsVisible = true;
                };

                tile.GestureRecognizers.Add(tap);

                Grid.SetRow(tile, row);
                Grid.SetColumn(tile, col);

                BoardGrid.Children.Add(tile);
            }
        }
    }

    // Fires once after the page loads, then unsubscribes itself
    private async void NewBoardPageLoaded(
        object? sender,
        EventArgs e) {
        Loaded -= NewBoardPageLoaded;
        await LoadExistingBoardAsync();
    }

    // Pulls saved categories/clues from the ViewModel into the UI (edit mode only)
    private async Task LoadExistingBoardAsync() {
        if (!_editingGameId.HasValue)
            return;

        try {
            await _viewModel.LoadExistingBoardAsync();

            for (int column = 0; column < 6; column++) {
                _categoryEntries[column].Text = _viewModel.GetCategoryName(column);

                for (int row = 0; row < 5; row++) {
                    if (!_viewModel.IsFilled(column, row))
                        continue;

                    // Mark already-filled cells with a checkmark instead of the dollar value
                    Label cellLabel = _cellLabels[column, row];
                    cellLabel.Text = "✓";
                    cellLabel.TextColor = Color.FromArgb("#FFD700");
                    cellLabel.FontSize = 24;
                }
            }
        }
        catch (Exception ex) {
            await DisplayAlertAsync(
                "Load Failed",
                ex.Message,
                "OK");

            await Navigation.PopAsync();
        }
    }

    // Closes the popup without saving, resets the tile's border
    private void ClosePopupTapped(
        object sender,
        TappedEventArgs e) {
        PopupOverlay.IsVisible = false;

        if (_selectedTile != null) {
            _selectedTile.StrokeThickness = 1;
            _selectedTile.Stroke = Color.FromArgb("#375D99");
            _selectedTile = null;
        }
    }

    // Validates and stores the question/answer for the selected cell (in memory, not DB yet)
    private async void SaveQuestionTapped(
    object sender,
    TappedEventArgs e) {
        string question = QuestionEditor.Text?.Trim() ?? string.Empty;
        string answer = AnswerEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question) ||
            string.IsNullOrWhiteSpace(answer)) {
            await DisplayAlertAsync(
                "Missing Information",
                "Please enter both the question and the correct answer.",
                "OK");

            return;
        }

        _viewModel.SaveClueInMemory(
            _selectedColumn,
            _selectedRow,
            question,
            answer);

        // Mark the cell as filled
        Label selectedLabel = _cellLabels[_selectedColumn, _selectedRow];
        selectedLabel.Text = "✓";
        selectedLabel.TextColor = Color.FromArgb("#FFD700");
        selectedLabel.FontSize = 24;

        PopupOverlay.IsVisible = false;

        if (_selectedTile != null) {
            _selectedTile.StrokeThickness = 1;
            _selectedTile.Stroke = Color.FromArgb("#375D99");
            _selectedTile = null;
        }
    }

    // Pushes category names into the ViewModel, then persists the whole board to the DB
    private async Task<int?> SaveBoardAsync() {
        try {
            for (int column = 0; column < 6; column++) {
                string categoryName = _categoryEntries[column].Text?.Trim() ?? string.Empty;
                _viewModel.SetCategoryName(column, categoryName);
            }

            return await _viewModel.SaveBoardAsync();
        }
        catch (Exception ex) {
            await DisplayAlertAsync(
                "Save Failed",
                ex.Message,
                "OK");

            return null;
        }
    }

    // Leaves without saving
    private async void CloseTapped(
        object sender,
        TappedEventArgs e) {
        await Navigation.PopAsync();
    }

    // Saves the board and returns to the previous page
    private async void SaveOnlyTapped(
        object sender,
        TappedEventArgs e) {
        int? gameId = await SaveBoardAsync();

        if (gameId == null)
            return;

        string message = _editingGameId.HasValue
            ? "Your saved board was updated successfully."
            : "Your custom board was saved successfully.";

        await DisplayAlertAsync(
            "Saved",
            message,
            "OK");

        await Navigation.PopAsync();
    }

    // Saves the board and moves straight into player setup
    private async void SaveStartTapped(
        object sender,
        TappedEventArgs e) {
        int? gameId = await SaveBoardAsync();

        if (gameId == null)
            return;

        await Navigation.PushAsync(
            new PlayerSetupPage(
                _dbService,
                new PlayerSetupViewModel(_dbService, new PlayerSetupService()),
                gameId.Value));
    }
}