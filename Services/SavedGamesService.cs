using jeo_ano_ba.Models;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Services;

public class SavedGamesService
{
    private readonly GameDatabaseService _dbService;

    public SavedGamesService(GameDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<List<SavedGameCardViewModel>> LoadSavedGameCardsAsync()
    {
        List<GameDb> allGames = await _dbService.GetAllGamesAsync();
        List<SavedGameCardViewModel> savedGames = new();

        foreach (GameDb game in allGames.Where(game => !game.IsPreset))
        {
            GameDb detailedGame = await _dbService.GetGameWithDetailsAsync(game.Id);

            bool hasUnfinishedClues = detailedGame.Categories
                .SelectMany(category => category.Clues)
                .Any(clue => !clue.IsCompleted);

            if (!hasUnfinishedClues)
            {
                await _dbService.DeleteGameAsync(game.Id);
                continue;
            }

            List<string> categoryNames = detailedGame.Categories
                .Select(category => category.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            savedGames.Add(new SavedGameCardViewModel
            {
                Id = game.Id,
                Name = game.Name,
                CategoryPreview = categoryNames.Count > 0
                    ? string.Join(" - ", categoryNames)
                    : "No categories"
            });
        }

        return savedGames
            .OrderByDescending(game => game.Id)
            .ToList();
    }

    public async Task DeleteGameAsync(int gameId)
    {
        await _dbService.DeleteGameAsync(gameId);
    }
}