using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba;

public partial class MainPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private readonly List<Player> _players;
    private readonly List<Button> _playerButtons = new();
    private int? _activePlayerIndex = null;
    private int? _autoLoadGameId;

    private ClueDb? _currentEvaluatingClue;

    public MainPage(GameDatabaseService dbService, List<Player> players, int? autoLoadGameId = null)
    {
        InitializeComponent();
        _dbService = dbService;
        _players = players.Count > 0 ? players : new List<Player> { new Player { Name = "Player 1" } };
        _autoLoadGameId = autoLoadGameId;

        WrapContentWithBuzzerBar();
    }

    // Wraps whatever Content was set in MainPage.xaml inside a Grid with the
    // original page on top and a row of player buzz-in buttons pinned to the
    // bottom. This is done entirely in code so MainPage.xaml never needs editing.
    private void WrapContentWithBuzzerBar()
    {
        var originalContent = Content;

        var wrapper = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        wrapper.Add(originalContent, 0, 0);
        wrapper.Add(BuildBuzzerBar(), 0, 1);

        Content = wrapper;
    }

    private Grid BuildBuzzerBar()
    {
        _playerButtons.Clear();

        var bar = new Grid
        {
            Padding = new Thickness(10),
            ColumnSpacing = 8,
            BackgroundColor = Color.FromArgb("#111111")
        };

        for (int i = 0; i < _players.Count; i++)
            bar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (int i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            var button = new Button
            {
                Text = $"{player.Name}\n${player.Score}",
                BackgroundColor = Color.FromArgb("#333333"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                Padding = new Thickness(4)
            };

            int capturedIndex = i; // avoid captured-variable bug in the loop
            button.Clicked += (s, e) => OnPlayerBuzzed(capturedIndex);

            _playerButtons.Add(button);
            bar.Add(button, i, 0);
        }

        return bar;
    }

    private void OnPlayerBuzzed(int index)
    {
        // Only the first player to buzz in on a given clue gets locked in.
        if (_currentEvaluatingClue == null) return;
        if (_activePlayerIndex != null) return;

        _activePlayerIndex = index;

        for (int i = 0; i < _playerButtons.Count; i++)
        {
            _playerButtons[i].BackgroundColor = i == index ? Color.FromArgb("#FF9800") : Color.FromArgb("#333333");
            _playerButtons[i].IsEnabled = (i == index);
        }
    }

    private void ResetBuzzers()
    {
        _activePlayerIndex = null;
        for (int i = 0; i < _playerButtons.Count; i++)
        {
            _playerButtons[i].IsEnabled = true;
            _playerButtons[i].BackgroundColor = Color.FromArgb("#333333");
            _playerButtons[i].Text = $"{_players[i].Name}\n${_players[i].Score}";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshGameListMenuAsync();

        if (_autoLoadGameId.HasValue)
        {
            var details = await _dbService.GetGameWithDetailsAsync(_autoLoadGameId.Value);
            BindableLayout.SetItemsSource(ProxyBoardGrid, details.Categories);
            _autoLoadGameId = null; // only auto-load once, on first appearance
        }
    }

    private async Task RefreshGameListMenuAsync()
    {
        GamesListView.ItemsSource = await _dbService.GetAllGamesAsync();
    }

    private async void OnCreateCustomGameClicked(object sender, EventArgs e)
    {
        try
        {
            string title = await DisplayPromptAsync("New Game", "Enter a name for your custom match:", initialValue: "Custom Game");
            if (string.IsNullOrWhiteSpace(title)) return;

            var categories = await _dbService.GetAvailableCategoriesAsync();

            // 🛠 FIXED: this was inverted before (was blocking the popup whenever
            // categories WERE found). Now it only bails out when there's nothing to pick from.
            if (categories.Count == 0)
            {
                await DisplayAlert("No Categories", "No categories were found in the database.", "OK");
                return;
            }

            var popup = new CategorySelectorPopup(categories);
            var result = await this.ShowPopupAsync(popup);
            if (result is List<string> selected && selected.Count == 5)
            {
                var gameId = await _dbService.BuildCustomGameFromCategoriesAsync(title, selected);
                await RefreshGameListMenuAsync();
                var details = await _dbService.GetGameWithDetailsAsync(gameId);
                BindableLayout.SetItemsSource(ProxyBoardGrid, details.Categories);
                await DisplayAlert("Success", "Game board compiled!", "Play");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnGameSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is GameDb selectedGame)
        {
            var details = await _dbService.GetGameWithDetailsAsync(selectedGame.Id);
            BindableLayout.SetItemsSource(ProxyBoardGrid, details.Categories);
        }
    }

    private void OnClueTileClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ClueDb clue })
        {
            _currentEvaluatingClue = clue;
            ModalClueLabel.Text = clue.Question;

            ModalAnswerLabel.IsVisible = false;
            PreAnswerButtonRow.IsVisible = true;
            PostAnswerButtonRow.IsVisible = false;

            EvaluationModal.IsVisible = true;
            ResetBuzzers(); // fresh clue = fresh buzz-in race
        }
    }

    private void OnShowAnswerClicked(object sender, EventArgs e)
    {
        if (_currentEvaluatingClue == null) return;

        ModalAnswerLabel.Text = _currentEvaluatingClue.Answer;
        ModalAnswerLabel.IsVisible = true;

        PreAnswerButtonRow.IsVisible = false;
        PostAnswerButtonRow.IsVisible = true;
    }
    private void OnPassClicked(object sender, EventArgs e)
    {
        // Closes the clue without scoring anyone — same as the old Cancel button.
        EvaluationModal.IsVisible = false;
        ResetBuzzers();
    }
    private async void OnCorrectClicked(object sender, EventArgs e)
    {
        await ResolveClueAsync(isCorrect: true);
    }

    private async void OnIncorrectClicked(object sender, EventArgs e)
    {
        await ResolveClueAsync(isCorrect: false);
    }

    private async Task ResolveClueAsync(bool isCorrect)
    {
        if (_currentEvaluatingClue == null) return;

        if (_activePlayerIndex == null)
        {
            await DisplayAlert("No Buzz-In", "A player needs to buzz in before you can score this clue.", "OK");
            return;
        }

        var activePlayer = _players[_activePlayerIndex.Value];

        if (isCorrect)
            activePlayer.Score += _currentEvaluatingClue.PointValue;
        else
            activePlayer.Score -= _currentEvaluatingClue.PointValue;

        _currentEvaluatingClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(_currentEvaluatingClue);

        EvaluationModal.IsVisible = false;
        ResetBuzzers(); // also refreshes each button's displayed score

        // Trigger UI update on the board grid
        var items = BindableLayout.GetItemsSource(ProxyBoardGrid);
        BindableLayout.SetItemsSource(ProxyBoardGrid, null);
        BindableLayout.SetItemsSource(ProxyBoardGrid, items);
    }
}