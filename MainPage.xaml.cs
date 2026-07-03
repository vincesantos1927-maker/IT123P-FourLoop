using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba;

public partial class MainPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private ClueDb? _currentEvaluatingClue;
    private int _scoreCounter = 0;

    public MainPage(GameDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshGameListMenuAsync();
    }

    private async Task RefreshGameListMenuAsync()
    {
        GamesListView.ItemsSource = await _dbService.GetAllGamesAsync();
    }

    // 🚀 CLEANED: Focuses only on local database generation
    private async void OnCreateCustomGameClicked(object sender, EventArgs e)
    {
        try
        {
            string title = await DisplayPromptAsync("New Game", "Enter a name for your custom match:", initialValue: "Custom Game");
            if (string.IsNullOrWhiteSpace(title)) return;

            var categories = await _dbService.GetAvailableCategoriesAsync();
            if (categories.Count == 0)
            {
                await DisplayAlert("Debug", $"Categories found in DB: {categories.Count}", "OK");
                return;
            }

            var popup = new CategorySelectorPopup(categories);
            var result = await this.ShowPopupAsync(popup);

            if (result is List<string> selected && selected.Count == 5)
            {
                await _dbService.BuildCustomGameFromCategoriesAsync(title, selected);
                await RefreshGameListMenuAsync();
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
            UserAnswerEntry.Text = string.Empty;
            EvaluationModal.IsVisible = true;
        }
    }

    private async void OnSubmitResponseClicked(object sender, EventArgs e)
    {
        if (_currentEvaluatingClue == null) return;

        bool isCorrect = UserAnswerEntry.Text?.Trim().Equals(_currentEvaluatingClue.Answer, StringComparison.OrdinalIgnoreCase) ?? false;

        if (isCorrect)
        {
            _scoreCounter += _currentEvaluatingClue.PointValue;
            await DisplayAlert("Correct!", "Great job.", "OK");
        }
        else
        {
            _scoreCounter -= _currentEvaluatingClue.PointValue;
            await DisplayAlert("Incorrect", $"Answer was: {_currentEvaluatingClue.Answer}", "OK");
        }

        ScoreHud.Text = $"SCORE: ${_scoreCounter}";
        _currentEvaluatingClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(_currentEvaluatingClue);

        EvaluationModal.IsVisible = false;
        // Trigger UI update
        var items = BindableLayout.GetItemsSource(ProxyBoardGrid);
        BindableLayout.SetItemsSource(ProxyBoardGrid, null);
        BindableLayout.SetItemsSource(ProxyBoardGrid, items);
    }

    private void OnCancelModalClicked(object sender, EventArgs e) => EvaluationModal.IsVisible = false;
}