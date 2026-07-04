using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba;

public partial class StartPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private readonly List<Entry> _nameEntries = new();

    public StartPage(GameDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        PlayerCountPicker.SelectedIndex = 1;
    }

    private void OnPlayerCountChanged(object? sender, EventArgs e)
    {
        PlayerNamesStack.Children.Clear();
        _nameEntries.Clear();

        int count = PlayerCountPicker.SelectedIndex + 1;
        for (int i = 1; i <= count; i++)
        {
            var entry = new Entry { Placeholder = $"Player {i} name", TextColor = Colors.White, PlaceholderColor = Colors.Gray };
            _nameEntries.Add(entry);
            PlayerNamesStack.Children.Add(entry);
        }
    }

    private List<Player> BuildPlayerList()
    {
        if (_nameEntries.Count == 0) OnPlayerCountChanged(null, EventArgs.Empty);

        var players = new List<Player>();
        for (int i = 0; i < _nameEntries.Count; i++)
        {
            string name = string.IsNullOrWhiteSpace(_nameEntries[i].Text) ? $"Player {i + 1}" : _nameEntries[i].Text.Trim();
            players.Add(new Player { Name = name, Score = 0 });
        }
        return players;
    }

    // "API GAME" button: picks categories from the pre-loaded jeopardy_clues.json library
    private async void OnApiGameClicked(object? sender, EventArgs e)
    {
        try
        {
            string title = await DisplayPromptAsync("New Game", "Enter a name for your match:", initialValue: "Custom Game");
            if (string.IsNullOrWhiteSpace(title)) return;

            var categories = await _dbService.GetAvailableCategoriesAsync();
            if (categories.Count == 0)
            {
                await DisplayAlert("No Categories", "No categories were found in the local database.", "OK");
                return;
            }

            var popup = new CategorySelectorPopup(categories);
            var result = await this.ShowPopupAsync(popup);

            if (result is List<string> selected && selected.Count == 5)
            {
                int gameId = await _dbService.BuildCustomGameFromCategoriesAsync(title, selected);
                await Navigation.PushAsync(new MainPage(_dbService, BuildPlayerList(), gameId));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // "CUSTOM GAME" button: player writes their own categories & questions, scoring is automatic
    private async void OnCustomGameClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new CustomGameBuilderPage(_dbService, BuildPlayerList()));
    }
}