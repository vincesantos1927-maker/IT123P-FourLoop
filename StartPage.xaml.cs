using CommunityToolkit.Maui.Views;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using jeo_ano_ba.Views;

namespace jeo_ano_ba;

public partial class StartPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
   

    public StartPage(GameDatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
    }


    

    // "API GAME" button: picks categories from the pre-loaded jeopardy_clues.json library
    private async void OnApiGameClicked(object? sender, EventArgs e)
    {
        try
        {
           
            var categories = await _dbService.GetAvailableCategoriesAsync();
            if (categories.Count == 0)
            {
                await DisplayAlertAsync("No Categories", "No categories were found in the local database.", "OK");
                return;
            }

            var popup = new CategorySelectorPopup(categories);
            var result = await this.ShowPopupAsync(popup);

            if (result is null)
                return;

            if (result is List<string> selected && selected.Count == 6)
            {
                int gameId = await _dbService.BuildCustomGameFromCategoriesAsync(
                    "Preset Game",
                    selected,
                    questionsPerCategory: 5,
                    startingPointValue: 100,
                    pointIncrement: 100);

                await Navigation.PushAsync(new PlayerSetupPage(_dbService, gameId));
            }

        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    // "CUSTOM GAME" button: player writes their own categories & questions, scoring is automatic
    private async void OnCustomGameClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.MainMenuPage(_dbService));
    }
}