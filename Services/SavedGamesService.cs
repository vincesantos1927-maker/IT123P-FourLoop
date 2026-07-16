using jeo_ano_ba.Models;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Services;
//responsible for loading, displaying saved games in progress
public class SavedGamesService
{
    //uses gamedatabase to talk to api database
    private readonly GameDatabaseService _dbService;
    //constructor
    public SavedGamesService(GameDatabaseService dbService)
    {
        _dbService = dbService;
    }
    //loads list of saved games to display Load Game screen
    public async Task<List<SavedGameCardViewModel>> LoadSavedGameCardsAsync()
    {
        //takes the basic overview of all games
        List<GameDb> allGames = await _dbService.GetAllGamesAsync();
        List<SavedGameCardViewModel> savedGames = new();

        foreach (GameDb game in allGames.Where(game => !game.IsPreset))
        {
            GameDb detailedGame = await _dbService.GetGameWithDetailsAsync(game.Id);

            //checks if theres unanswered questions left
            //selectmany turns all clues to a single list
            bool hasUnfinishedClues = detailedGame.Categories
                .SelectMany(category => category.Clues)
                .Any(clue => !clue.IsCompleted);
            //once everyquestion in a game gets answered, it deletes the game everytime its done
            if (!hasUnfinishedClues)
            {
                await _dbService.DeleteGameAsync(game.Id);
                continue;
            }
            //Extart names of the categories to use as preview
            List<string> categoryNames = detailedGame.Categories
                .Select(category => category.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
            //turns database model into ui view model
            savedGames.Add(new SavedGameCardViewModel
            {
                Id = game.Id,
                Name = game.Name,
                //combines category names into 
                CategoryPreview = categoryNames.Count > 0
                    ? string.Join(" - ", categoryNames)
                    : "No categories"
            });
        }
        //returns sorted list
        return savedGames
            .OrderByDescending(game => game.Id)
            .ToList();
    }
    //closes saved game from the database
    public async Task DeleteGameAsync(int gameId)
    {
        await _dbService.DeleteGameAsync(gameId);
    }
}