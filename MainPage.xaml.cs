using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.ViewModels;
using jeo_ano_ba.Views;
namespace jeo_ano_ba;

public partial class MainPage : ContentPage {
    private readonly GameDatabaseService _dbService; // talks to the database for the categories, clues, and saved games
    private readonly GameBoardViewModel _viewModel; // lets the VM remember the current state of a game 
    private readonly List<Button> _buzzButtons = new(); 
    private readonly List<(Border chip, Label label)> _scoreLabels = new();
    private int? _autoLoadGameId;
    private readonly GameTimerService _timerService; // timerservice for the countdown
    private readonly BgmService _bgmService = IPlatformApplication.Current!.Services.GetRequiredService<BgmService>(); // bgm in bgmservice 
    private readonly SfxService _sfxService = IPlatformApplication.Current!.Services.GetRequiredService<SfxService>();
    private readonly string _boardName;

    // colors of players are reused in buzzers, scoreboard, and leaderboards
    private static readonly string[] PlayerColors =
        { "#E74C3C", "#3498DB", "#2ECC71", "#9B59B6" }; 

    // initializes the gameplay page, VM, services, and UI
    public MainPage( 
        GameDatabaseService dbService,
        List<Player> players,
        int? autoLoadGameId = null,
        int timerSeconds = 30,
        string boardName = "",
        GameTimerService? timerService = null) {
        InitializeComponent();
        _dbService = dbService;
        _viewModel = new GameBoardViewModel(dbService, players, timerSeconds);
        _autoLoadGameId = autoLoadGameId;
        _boardName = boardName;
        _timerService = timerService ?? new GameTimerService();
        BindingContext = _viewModel; 
        _timerService.Tick += OnTimerTick;
        _timerService.TimedOut += OnTimerTimedOut;

        WrapContentWithScoreFooter(); 
        BuildBuzzInGrid();
        UpdateMusicButtonIcon();
    } 

    // updates the bgm icon based on the current BGM state
    private void UpdateMusicButtonIcon() {
        MusicToggleButton.Text = _bgmService.IsEnabled ? "🔊" : "🔇";
    }

    // toggles the BGM state and updates the icon when the music button is clicked
    private void OnMusicToggleClicked(object sender, EventArgs e) {
        _bgmService.Toggle();
        UpdateMusicButtonIcon();
    }

    // this method wraps the existing gameplay content inside a new layout and adds a dynamic score footer at the bottom of page
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

    // this returns a formatted string rep of a player's score by forwarding it to VM
    private static string FormatScore(int score) => GameBoardViewModel.FormatScore(score);

    // this builds the score footer that shows each player's name and score in a colored card
    private Grid BuildScoreFooter() { 
        _scoreLabels.Clear(); // clears score labels every time the method runs

        // container that holds the player score cards
        var bar = new Grid {
            Padding = new Thickness(10),
            ColumnSpacing = 8,
            BackgroundColor = Color.FromArgb("#0F0F2D")
        };

        // creates one column for each player
        for (int i = 0; i < _viewModel.Players.Count; i++)
            bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        // iterates per player
        for (int i = 0; i < _viewModel.Players.Count; i++) {
            var player = _viewModel.Players[i]; 
            string color = PlayerColors[i % PlayerColors.Length];

            // creates the colored score card
            var chip = new Border {
                BackgroundColor = Color.FromArgb(color),
                Padding = new Thickness(6),
                HeightRequest = 50,
                StrokeShape = new RoundRectangle { CornerRadius = 12 }
            };

            // text inside the card
            var label = new Label {
                Text = $"{player.Name}\n{FormatScore(player.Score)}",
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            chip.Content = label; // puts label inside the border

            _scoreLabels.Add((chip, label));
            Grid.SetColumn(chip, i);
            bar.Add(chip, i, 0);
        }

        return bar;
    }

    // this builds the buzz-in grid that shows each player's buzzer button
    private void BuildBuzzInGrid() {
        _buzzButtons.Clear(); // clears the list of buzz buttons every time the method runs
        BuzzInGrid.Children.Clear();

        // creates columns for the buzz-in grid
        for (int i = 0; i < _viewModel.Players.Count; i++) {
            var player = _viewModel.Players[i];
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

    // handles the event when a player buzzes in, stops the timer, and starts the countdown for answering
    private async void OnPlayerBuzzed(int index)
    {
        // ignore if no clue is selected or another player is already answering
        if (_viewModel.CurrentClue == null) return;
        if (_viewModel.ActivePlayerIndex != null) return;

        _sfxService.PlayBuzzer();

        _timerService.Stop();
        _sfxService.StopTicking();
        _viewModel.BuzzIn(index);

        MainThread.BeginInvokeOnMainThread(() => {
            SkipButton.IsVisible = false;
            WhoKnowsLabel.IsVisible = false;
            PreAnswerButtonRow.IsVisible = true;
            TimerLabel.IsVisible = true;
            TimerBar.IsVisible = true;
            TimerBar.Progress = 1;
            TimerLabel.Text = $"{_viewModel.TimerSeconds}s";

            // player who buzzed in is higlighted and other player's buzzers are disabled
            for (int i = 0; i < _buzzButtons.Count; i++)
            {
                bool isActive = i == index;
                _buzzButtons[i].Opacity = isActive ? 1.0 : 0.3;
                _buzzButtons[i].IsEnabled = false;
                _buzzButtons[i].Text = isActive ? $"{_viewModel.Players[i].Name}\nAnswering..." : _viewModel.Players[i].Name;
            }
        });

        _sfxService.PlayTicking();
        await _timerService.StartAsync(_viewModel.TimerSeconds);
    }

    // this builds the game board using the loaded categs and clues
    private void PopulateBoard(List<CategoryDb> categories) {

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


    // refreshes the game board 
    private void RefreshBoard()
    {
        if (_viewModel.Categories != null)
            PopulateBoard(_viewModel.Categories);
    }

    // resets the buzzers and score labels to their initial state for the next clue
    private void ResetBuzzers() {

        WhoKnowsLabel.IsVisible = true;

        // resets the buzzers to their initial state
        for (int i = 0; i < _buzzButtons.Count; i++) {
            _buzzButtons[i].IsEnabled = true;
            _buzzButtons[i].Opacity = 1.0;
            _buzzButtons[i].Text = _viewModel.Players[i].Name;
        }

        // updates the score labels to reflect the current scores and highlights the last player who picked a clue
        for (int i = 0; i < _scoreLabels.Count; i++) {
            _scoreLabels[i].label.Text = $"{_viewModel.Players[i].Name}\n{GameBoardViewModel.FormatScore(_viewModel.Players[i].Score)}";
            _scoreLabels[i].chip.StrokeThickness = (_viewModel.LastPickerIndex.HasValue && i == _viewModel.LastPickerIndex.Value) ? 3 : 0;
            _scoreLabels[i].chip.Stroke = Colors.White;
        }

        SkipButton.IsVisible = true;
    }

    // runs every time the gameplay page becomes visible
    protected override async void OnAppearing() {
        base.OnAppearing();
        await RefreshGameListMenuAsync();

        // automatically load the selected game when opening the page
        if (_autoLoadGameId.HasValue)
        {
            // save the board name if provided
            if (!string.IsNullOrWhiteSpace(_boardName))
            {
                await _dbService.UpdateGameNameAsync(_autoLoadGameId.Value, _boardName);
            }

            // load selected game into VM
            await _viewModel.LoadGameAsync(_autoLoadGameId.Value);

            // build game board after loading categs
            if (_viewModel.Categories != null)
                PopulateBoard(_viewModel.Categories);

            _autoLoadGameId = null;
        }
    }

    // refreshes the list of saved games from the database
    private async Task RefreshGameListMenuAsync() {
        GamesListView.ItemsSource = await _dbService.GetAllGamesAsync();
    }

    // creates a new custom game by prompting for a title, selecting categories, and building the game in the database
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

    // loads the selected game from the database and populates the game board
    private async void OnGameSelected(object sender, SelectedItemChangedEventArgs e) {
        if (e.SelectedItem is GameDb selectedGame)
        {
            await _viewModel.LoadGameAsync(selectedGame.Id);

            if (_viewModel.Categories != null)
                PopulateBoard(_viewModel.Categories);
        }
    }

    // handles the event when a clue tile is clicked, shows the clue modal, and resets buzzers
    private void OnClueTileClicked(object sender, EventArgs e) {
        if (sender is Button { CommandParameter: ClueDb clue }) {
            _viewModel.SelectClue(clue); ;
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
            _sfxService.StopTicking();
            EvaluationModal.IsVisible = true;
            ResetBuzzers();
        }
    }

    // handles the event when the "Show Answer" button is clicked, stops the timer, and displays the answer modal
    private void OnShowAnswerClicked(object sender, EventArgs e) {
        if (_viewModel.CurrentClue == null) return;

        _timerService.Stop();
        _sfxService.StopTicking();
        TimerLabel.IsVisible = false;
        TimerBar.IsVisible = false;

        ModalAnswerLabel.Text = _viewModel.CurrentClue.Answer;
        ModalAnswerLabel.IsVisible = true;

        PreAnswerButtonRow.IsVisible = false;
        PostAnswerButtonRow.IsVisible = true;

        if (_viewModel.ActivePlayerIndex.HasValue)
        {
            int index = _viewModel.ActivePlayerIndex.Value;
            _buzzButtons[index].Text = _viewModel.Players[index].Name;
        }
    }

    // handles the event when the "Skip" button is clicked, stops the timer, skips the current clue, and refreshes the board
    private async void OnSkipClicked(object sender, EventArgs e) {
        if (_viewModel.CurrentClue == null) return;
        if (_viewModel.ActivePlayerIndex != null) return;

        _timerService.Stop();
        _sfxService.StopTicking();
        await _viewModel.SkipCurrentClueAsync();

        EvaluationModal.IsVisible = false;
        ResetBuzzers();
        RefreshBoard();
        CheckGameComplete();
    }

    // marks answer as correct
    private async void OnCorrectClicked(object sender, EventArgs e) {
        _sfxService.PlayCorrect();
        await ResolveClueAsync(isCorrect: true);
    }

    // marks answer as incorrect
    private async void OnIncorrectClicked(object sender, EventArgs e) {
        _sfxService.PlayWrong();
        await ResolveClueAsync(isCorrect: false);
    }

    // finishes the current clue and prepares the next turn
    private async void OnProceedClicked(object sender, EventArgs e) {
        if(_viewModel.CurrentClue == null) return;

        await _viewModel.FinishCurrentClueAsync();

        // reset gameplay UI and refresh the board
        EvaluationModal.IsVisible = false;
        ResetBuzzers();
        RefreshBoard();
        CheckGameComplete();
    }

    // resolves current clue based on whether the answer is correct
    private async Task ResolveClueAsync(bool isCorrect) {
        if (_viewModel.CurrentClue == null) return;
        if (_viewModel.ActivePlayerIndex == null) {
            await DisplayAlert("No Buzz-In", "A player needs to buzz in before you can score this clue.", "OK");
            return;
        }

        // update the clue result and player score
        await _viewModel.ResolveCurrentClueAsync(isCorrect);

        // reset the gameplay UI and refresh the board
        EvaluationModal.IsVisible = false;
        ResetBuzzers();
        RefreshBoard();
        CheckGameComplete();
    }

    // ends the game and returns to main menu
    private async void OnEndGameClicked(object sender, EventArgs e) {
        await Navigation.PopToRootAsync();
    }

    // updates the coundown timer and progress bar
    private void OnTimerTick(int seconds) {
        MainThread.BeginInvokeOnMainThread(() => {
            TimerLabel.Text = $"{seconds}s";
            TimerBar.ProgressTo((double)seconds / _viewModel.TimerSeconds, 900, Easing.Linear);
        });
    }

    // handles the event when the timer runs out, applies a penalty, and shows the correct answer
    private async void OnTimerTimedOut() {

        // ignore if no clue is active or no player has buzzed in
        if (_viewModel.CurrentClue == null || _viewModel.ActivePlayerIndex == null)
            return;

        // apply timeout penalty to the active player
        _sfxService.StopTicking();
        _sfxService.PlayWrong();

        string answer = _viewModel.CurrentClue.Answer;
        await _viewModel.ApplyTimeoutPenaltyAsync();

        MainThread.BeginInvokeOnMainThread(() => {
            // reveal the correct answer and update the gameplay controls
            ModalAnswerLabel.Text = answer;
            ModalAnswerLabel.IsVisible = true;

            TimerLabel.IsVisible = false;
            TimerBar.IsVisible = false;
            PreAnswerButtonRow.IsVisible = false;
            PostAnswerButtonRow.IsVisible = false;
            ProceedButton.IsVisible = true;
        });
    }

    // ---
    // this part to the last method is for the leaderboards
    // ---

    // checks if the game is complete and shows the game over scoreboard if it is
    private void CheckGameComplete()
    {
        if (_viewModel.IsGameComplete())
            ShowGameOverScoreboard();
    }

    // returns the color associated with a player based on their index in the player list
    private Color GetPlayerColor(Player player) {
        int idx = _viewModel.Players.IndexOf(player);
        if (idx < 0) idx = 0;
        return Color.FromArgb(PlayerColors[idx % PlayerColors.Length]);
    }


    // displays the game over scoreboard with the winner and ranked players
    private void ShowGameOverScoreboard() {
        var ranked = _viewModel.GetRankedPlayers();
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


    // builds a single row in the game over scoreboard showing the player's rank, name, and score
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


    // handles the event when the "Back to Menu" button is clicked, hides the game over modal, and navigates back to the main menu
    private async void OnBoardEndActionClicked(object sender, EventArgs e) {
        BoardEndModal.IsVisible = false;
        await Navigation.PopToRootAsync();
    }
}