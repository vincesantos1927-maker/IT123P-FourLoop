using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;

public class SavedGameCardViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DateText { get; set; } = "Date created unavailable";

    public string CategoryPreview { get; set; } = "No categories";
}

public class SavedGamesViewModel : BaseViewModel
{
    private readonly SavedGamesService _savedGamesService;

    public SavedGamesViewModel(SavedGamesService savedGamesService)
    {
        _savedGamesService = savedGamesService;
    }

    public async Task<List<SavedGameCardViewModel>> LoadSavedGameCardsAsync()
    {
        return await _savedGamesService.LoadSavedGameCardsAsync();
    }

    public async Task DeleteGameAsync(int gameId)
    {
        await _savedGamesService.DeleteGameAsync(gameId);
    }
}