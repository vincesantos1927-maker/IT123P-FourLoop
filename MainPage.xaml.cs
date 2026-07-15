using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;

namespace jeo_ano_ba;

public partial class MainPage : ContentPage {
    private readonly GameDatabaseService _dbService;
    private readonly List<Player> _players;
    private readonly List<Button> _buzzButtons = new();
    private readonly List<(Border chip, Label label)> _scoreLabels = new();
    private int? _activePlayerIndex = null;
    private int? _lastPickerIndex = null;
    private int? _autoLoadGameId;
    private readonly GameTimerService _timerService = new();
    private readonly int _timerSeconds;
    private readonly string _boardName; // 1. Added variable to save the board name

    private ClueDb? _currentEvaluatingClue;
    private List<CategoryDb>? _currentCategories;

    private static readonly string[] PlayerColors =
        { "#E74C3C", "#3498DB", "#2ECC71", "#9B59B6" };

    // 2. Updated the constructor to accept the 5th parameter (boardName)
    public MainPage(
        GameDatabaseService dbService,
        List<Player> players,
        int? autoLoadGameId = null,
        int timerSeconds = 30,
        string boardName = "") {
        InitializeComponent();
        _dbService = dbService;
        _players = players.Count > 0 ? players : new List<Player> { new Player { Name = "Player 1" } };
        _autoLoadGameId = autoLoadGameId;
        _timerSeconds = timerSeconds;
        _boardName = boardName; // 3. Save the board name here
        _timerService.Tick += OnTimerTick;
        _timerService.TimedOut += OnTimerTimedOut;

        WrapContentWithScoreFooter();
        BuildBuzzInGrid();

        // Note: If you have a Label in your MainPage.xaml to display the board name, 
        // you can set it here, for example:
        // YourBoardNameLabel.Text = _boardName;
    }

    private void WrapContentWithScoreFooter() {
        var originalContent = Content;

        var wrapper = new Grid {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        wrapper.Add(originalContent, 0, 0);
        wrapper.Add(BuildScoreFooter(), 0, 1);

        Content = wrapper;
    }

    private static string FormatScore(int score) => score < 0 ? $"-${Math.Abs(score)}" : $"${score}";

    private Grid BuildScoreFooter() {
        _scoreLabels.Clear();

        var bar = new Grid {
            Padding = new Thickness(10),
            ColumnSpacing = 8,
            BackgroundColor = Color.FromArgb("#0F0F2D")
        };

        for (int i = 0; i < _players.Count; i++)
            bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (int i = 0; i < _players.Count; i++) {
            var player = _players[i];
            string color = PlayerColors[i % PlayerColors.Length];

            var chip = new Border {
                BackgroundColor = Color.FromArgb(color),
                Padding = new Thickness(6),
                HeightRequest = 50,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };

            var label = new Label {
                Text = $"{player.Name}\n{FormatScore(player.Score)}",
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            chip.Content = label;

            _scoreLabels.Add((chip, label));
            Grid.SetColumn(chip, i);
            bar.Add(chip, i, 0);
        }

        return bar;
    }

    private void BuildBuzzInGrid() {
        _buzzButtons.Clear();
        BuzzInGrid.Children.Clear();

        for (int i = 0; i < _players.Count; i++) {
            var player = _players[i];
            string color = PlayerColors[i % PlayerColors.Length];

            var button = new Button {
                Text = player.Name,
                BackgroundColor = Color.FromArgb(color),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                CornerRadius = 20,
                HeightRequest = 110
            };

            int capturedIndex = i;
            button.Clicked += (s, e) => OnPlayerBuzzed(capturedIndex);

            _buzzButtons.Add(button);

            int row = i / 2;
            int col = i % 2;
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);
            BuzzInGrid.Children.Add(button);
        }
    }

    private async void OnPlayerBuzzed(int index) {
        if (_currentEvaluatingClue == null) return;
        if (_activePlayerIndex != null) return;

        _timerService.Stop();
        _activePlayerIndex = index;

        MainThread.BeginInvokeOnMainThread(() => {
            SkipButton.IsVisible = false;
            WhoKnowsLabel.IsVisible = false;
            PreAnswerButtonRow.IsVisible = true;
            TimerLabel.IsVisible = true;
            TimerBar.IsVisible = true;
            TimerBar.Progress = 1;
            TimerLabel.Text = $"{_timerSeconds}s";

            for (int i = 0; i < _buzzButtons.Count; i++) {
                bool isActive = i == index;
                _buzzButtons[i].Opacity = isActive ? 1.0 : 0.3;
                _buzzButtons[i].IsEnabled = false; // lock all buttons once someone buzzes
                _buzzButtons[i].Text = isActive ? $"{_players[i].Name}\nAnswering..." : _players[i].Name;
            }
        });

        await _timerService.StartAsync(_timerSeconds);
    }

    private void PopulateBoard(List<CategoryDb> categories) {
        _currentCategories = categories;

        ProxyBoardGrid.Children.Clear();
        ProxyBoardGrid.ColumnDefinitions.Clear();
        ProxyBoardGrid.RowDefinitions.Clear();

        ProxyBoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int r = 0; r < 5; r++)
            ProxyBoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        for (int col = 0; col < categories.Count; col++) {
            ProxyBoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var category = categories[col];

            var headerBorder = new Border {
                BackgroundColor = Color.FromArgb("#1E1E3F"),
                Stroke = Color.FromArgb("#FFCC00"),
                StrokeThickness = 1,
                Padding = 4,
                HeightRequest = 60,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };
            headerBorder.Content = new Label {
                Text = category.Name,
                TextColor = Color.FromArgb("#FFCC00"),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                FontSize = 10,
                LineBreakMode = LineBreakMode.WordWrap
            };
            Grid.SetRow(headerBorder, 0);
            Grid.SetColumn(headerBorder, col);
            ProxyBoardGrid.Children.Add(headerBorder);

            for (int r = 0; r < category.Clues.Count; r++) {
                var clue = category.Clues[r];
                clue.CategoryName = category.Name;

                var button = new Button {
                    Text = clue.IsCompleted ? "" : $"${clue.PointValue}",
                    CommandParameter = clue,
                    CornerRadius = 10,
                    Padding = 2,
                    FontSize = 14,
                    BackgroundColor = clue.IsCompleted ? Color.FromArgb("#1E1E3F") : Color.FromArgb("#FFCC00"),
                    TextColor = Color.FromArgb("#0F0F2D"),
                    FontAttributes = FontAttributes.Bold,
                    IsEnabled = !clue.IsCompleted
                };
                button.Clicked += OnClueTileClicked;

                Grid.SetRow(button, r + 1);
                Grid.SetColumn(button, col);
                ProxyBoardGrid.Children.Add(button);
            }
        }
    }

    private void RefreshBoard() {
        if (_currentCategories != null)
            PopulateBoard(_currentCategories);
    }

    private void ResetBuzzers() {
        _activePlayerIndex = null;
        WhoKnowsLabel.IsVisible = true;

        for (int i = 0; i < _buzzButtons.Count; i++) {
            _buzzButtons[i].IsEnabled = true;
            _buzzButtons[i].Opacity = 1.0;
            _buzzButtons[i].Text = _players[i].Name;
        }

        for (int i = 0; i < _scoreLabels.Count; i++) {
            _scoreLabels[i].label.Text = $"{_players[i].Name}\n{FormatScore(_players[i].Score)}";
            _scoreLabels[i].chip.StrokeThickness = (_lastPickerIndex.HasValue && i == _lastPickerIndex.Value) ? 3 : 0;
            _scoreLabels[i].chip.Stroke = Colors.White;
        }

        SkipButton.IsVisible = true;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        await RefreshGameListMenuAsync();

        if (_autoLoadGameId.HasValue) {
            var details = await _dbService.GetGameWithDetailsAsync(_autoLoadGameId.Value);
            PopulateBoard(details.Categories);
            _autoLoadGameId = null;
        }
    }

    private async Task RefreshGameListMenuAsync() {
        GamesListView.ItemsSource = await _dbService.GetAllGamesAsync();
    }

    private async void OnCreateCustomGameClicked(object sender, EventArgs e) {
        try {
            string title = await DisplayPromptAsync("New Game", "Enter a name for your custom match:", initialValue: "Custom Game");
            if (string.IsNullOrWhiteSpace(title)) return;

            var categories = await _dbService.GetAvailableCategoriesAsync();

            if (categories.Count == 0) {
                await DisplayAlert("No Categories", "No categories were found in the database.", "OK");
                return;
            }

            var popup = new CategorySelectorPopup(categories);
            var result = await this.ShowPopupAsync(popup);
            if (result is List<string> selected && selected.Count == 6) {
                var gameId = await _dbService.BuildCustomGameFromCategoriesAsync(title, selected);
                await RefreshGameListMenuAsync();
                var details = await _dbService.GetGameWithDetailsAsync(gameId);
                PopulateBoard(details.Categories);
                await DisplayAlert("Success", "Game board compiled!", "Play");
            }
        }
        catch (Exception ex) {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnGameSelected(object sender, SelectedItemChangedEventArgs e) {
        if (e.SelectedItem is GameDb selectedGame) {
            var details = await _dbService.GetGameWithDetailsAsync(selectedGame.Id);
            PopulateBoard(details.Categories);
        }
    }

    private void OnClueTileClicked(object sender, EventArgs e) {
        if (sender is Button { CommandParameter: ClueDb clue }) {
            _currentEvaluatingClue = clue;
            ModalCategoryLabel.Text = clue.CategoryName.ToUpper();
            ModalPointValueLabel.Text = $"${clue.PointValue}";
            ModalClueLabel.Text = clue.Question;

            ModalAnswerLabel.IsVisible = false;
            PreAnswerButtonRow.IsVisible = false;
            PostAnswerButtonRow.IsVisible = false;
            ProceedButton.IsVisible = false;
            TimerLabel.IsVisible = false;
            TimerBar.IsVisible = false;
            TimerLabel.Text = "";
            _timerService.Stop();
            EvaluationModal.IsVisible = true;
            ResetBuzzers();
        }
    }

    private void OnShowAnswerClicked(object sender, EventArgs e) {
        if (_currentEvaluatingClue == null) return;

        _timerService.Stop();
        TimerLabel.IsVisible = false;
        TimerBar.IsVisible = false;

        ModalAnswerLabel.Text = _currentEvaluatingClue.Answer;
        ModalAnswerLabel.IsVisible = true;

        PreAnswerButtonRow.IsVisible = false;
        PostAnswerButtonRow.IsVisible = true;

        if (_activePlayerIndex.HasValue) {
            _buzzButtons[_activePlayerIndex.Value].Text = _players[_activePlayerIndex.Value].Name;
        }
    }

    private async void OnSkipClicked(object sender, EventArgs e) {
        if (_currentEvaluatingClue == null) return;
        if (_activePlayerIndex != null) return;

        _timerService.Stop();
        _currentEvaluatingClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(_currentEvaluatingClue);
        _currentEvaluatingClue = null;

        EvaluationModal.IsVisible = false;
        ResetBuzzers();
        RefreshBoard();
        CheckGameComplete();
    }

    private async void OnCorrectClicked(object sender, EventArgs e) {
        await ResolveClueAsync(isCorrect: true);
    }

    private async void OnIncorrectClicked(object sender, EventArgs e) {
        await ResolveClueAsync(isCorrect: false);
    }

    private async void OnProceedClicked(object sender, EventArgs e) {
        if (_currentEvaluatingClue == null) return;

        _lastPickerIndex = _activePlayerIndex;

        _currentEvaluatingClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(_currentEvaluatingClue);
        _currentEvaluatingClue = null;

        EvaluationModal.IsVisible = false;
        ResetBuzzers();
        RefreshBoard();
        CheckGameComplete();
    }

    private async Task ResolveClueAsync(bool isCorrect) {
        if (_currentEvaluatingClue == null) return;
        if (_activePlayerIndex == null) {
            await DisplayAlert("No Buzz-In", "A player needs to buzz in before you can score this clue.", "OK");
            return;
        }

        var activePlayer = _players[_activePlayerIndex.Value];

        if (isCorrect)
            activePlayer.Score += _currentEvaluatingClue.PointValue;
        else
            activePlayer.Score -= _currentEvaluatingClue.PointValue;

        _lastPickerIndex = _activePlayerIndex;

        _currentEvaluatingClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(_currentEvaluatingClue);
        _currentEvaluatingClue = null;

        EvaluationModal.IsVisible = false;
        ResetBuzzers();
        RefreshBoard();
        CheckGameComplete();
    }

    private async void OnEndGameClicked(object sender, EventArgs e) {
        await Navigation.PopToRootAsync();
    }

    private void OnTimerTick(int seconds) {
        MainThread.BeginInvokeOnMainThread(() => {
            TimerLabel.Text = $"{seconds}s";
            TimerBar.ProgressTo((double)seconds / _timerSeconds, 900, Easing.Linear);
        });
    }

    private async void OnTimerTimedOut() {
        if (_currentEvaluatingClue == null || _activePlayerIndex == null)
            return;

        var activePlayer = _players[_activePlayerIndex.Value];
        activePlayer.Score -= _currentEvaluatingClue.PointValue;

        MainThread.BeginInvokeOnMainThread(() => {
            ModalAnswerLabel.Text = _currentEvaluatingClue.Answer;
            ModalAnswerLabel.IsVisible = true;

            TimerLabel.IsVisible = false;
            TimerBar.IsVisible = false;
            PreAnswerButtonRow.IsVisible = false;
            PostAnswerButtonRow.IsVisible = false;
            ProceedButton.IsVisible = true;
        });

        _currentEvaluatingClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(_currentEvaluatingClue);
    }

    // ============================================================
    // LEADERBOARD / GAME OVER
    // ============================================================

    private void CheckGameComplete() {
        if (_currentCategories == null) return;

        bool allDone = _currentCategories
            .SelectMany(c => c.Clues)
            .All(clue => clue.IsCompleted);

        if (allDone)
            ShowGameOverScoreboard();
    }

    private Color GetPlayerColor(Player player) {
        int idx = _players.IndexOf(player);
        if (idx < 0) idx = 0;
        return Color.FromArgb(PlayerColors[idx % PlayerColors.Length]);
    }

    private void ShowGameOverScoreboard() {
        var ranked = _players.OrderByDescending(p => p.Score).ToList();
        if (ranked.Count == 0) return;

        BoardEndContentHost.Children.Clear();
        WinnerCardHost.Children.Clear();

        var winner = ranked[0];

        // Winner spotlight card
        WinnerCardHost.Children.Add(new Label {
            Text = "👑",
            FontSize = 28,
            HorizontalTextAlignment = TextAlignment.Center
        });
        WinnerCardHost.Children.Add(new Label {
            Text = winner.Name,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 26,
            HorizontalTextAlignment = TextAlignment.Center
        });
        WinnerCardHost.Children.Add(new Label {
            Text = FormatScore(winner.Score),
            TextColor = Color.FromArgb("#FFFFFF"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 28,
            HorizontalTextAlignment = TextAlignment.Center
        });
        WinnerCardHost.Children.Add(new Label {
            Text = "WINNER!",
            TextColor = Color.FromArgb("#FFFFFF"),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1,
            HorizontalTextAlignment = TextAlignment.Center
        });

        // Remaining players, ranked #2 onward
        for (int i = 1; i < ranked.Count; i++) {
            BoardEndContentHost.Children.Add(BuildRankRow(i + 1, ranked[i]));
        }

        BoardEndModal.IsVisible = true;
    }

    private Border BuildRankRow(int rank, Player player) {
        var row = new Grid {
            ColumnDefinitions = { new(30), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 12,
            Padding = new Thickness(6, 0)
        };

        var rankLabel = new Label {
            Text = rank.ToString(),
            TextColor = Color.FromArgb("#4C6587"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        };

        var colorCircle = new Border {
            WidthRequest = 32,
            HeightRequest = 32,
            BackgroundColor = GetPlayerColor(player),
            StrokeThickness = 0,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) }
        };

        var nameLabel = new Label {
            Text = player.Name,
            TextColor = Colors.White,
            FontSize = 17,
            VerticalOptions = LayoutOptions.Center
        };

        var scoreLabel = new Label {
            Text = FormatScore(player.Score),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 17,
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetColumn(rankLabel, 0);
        Grid.SetColumn(colorCircle, 1);
        Grid.SetColumn(nameLabel, 2);
        Grid.SetColumn(scoreLabel, 3);
        row.Children.Add(rankLabel);
        row.Children.Add(colorCircle);
        row.Children.Add(nameLabel);
        row.Children.Add(scoreLabel);

        return new Border {
            BackgroundColor = Color.FromArgb("#13294B"),
            StrokeThickness = 0,
            Padding = new Thickness(14, 12),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
            Content = row
        };
    }

    private async void OnBoardEndActionClicked(object sender, EventArgs e) {
        BoardEndModal.IsVisible = false;
        await Navigation.PopToRootAsync();
    }
}