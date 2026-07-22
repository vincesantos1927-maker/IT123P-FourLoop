using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;
//gameplay viewmodel, manages the game
public class GameBoardViewModel : BaseViewModel
{
    
    private readonly GameDatabaseService _dbService;
    //sets the match with players and timer config
    public GameBoardViewModel(GameDatabaseService dbService, List<Player> players, int timerSeconds)
    {
        _dbService = dbService;
        //no players are passed, falls back to single default player
        Players = players.Count > 0 ? players : new List<Player> { new Player { Name = "Player 1" } };
        TimerSeconds = timerSeconds;
    }
    //list of players
    public List<Player> Players { get; }
    //list of categoris
    public List<CategoryDb>? Categories { get; private set; }
    //how many seconds are left per player
    public int TimerSeconds { get; }
    //current clue/question being flasshed on screen
    public ClueDb? CurrentClue { get; private set; }

    //index of who currently is buzzed
    public int? ActivePlayerIndex { get; private set; }
    //tracks player who was last buzzed
    public int? LastPickerIndex { get; private set; }
    //loads game ategories and aswers
    public async Task LoadGameAsync(int gameId)
    {
        GameDb game = await _dbService.GetGameWithDetailsAsync(gameId);
        Categories = game.Categories;
        //notify ui that categories are chosen so it can build the game now
        OnPropertyChanged(nameof(Categories));
    }
    //whenever a player clicks a clue it opens the clue view and resets the active buzzer
    public void SelectClue(ClueDb clue)
    {
        CurrentClue = clue;
        ActivePlayerIndex = null;//no one is buzzed yet for the fresh clue
        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
    }
    //locks a player when they hit their button
    public void BuzzIn(int playerIndex)
    {
        //prevents buzzing if someone else is buzzed or theres no question active
        if (CurrentClue == null || ActivePlayerIndex != null)
            return;

        ActivePlayerIndex = playerIndex;
        OnPropertyChanged(nameof(ActivePlayerIndex));
    }
    //skips current question, marks it as completed
    public async Task SkipCurrentClueAsync()
    {
        if (CurrentClue == null)
            return;
        //marks as finished and save state to serevr
        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);
        //reset state back to board view
        CurrentClue = null;
        ActivePlayerIndex = null;

        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
    }
    //deducts player buzzed when time runs out
    public async Task ApplyTimeoutPenaltyAsync()
    {
        if (CurrentClue == null || ActivePlayerIndex == null)
            return;

        Player player = Players[ActivePlayerIndex.Value];
        player.Score -= CurrentClue.PointValue;
        //records as last person to make a move
        LastPickerIndex = ActivePlayerIndex;
        //marks it as done
        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);

        OnPropertyChanged(nameof(LastPickerIndex));
    }
    //closes out the question after final decision has been made
    public async Task FinishCurrentClueAsync()
    {
        if (CurrentClue == null)
            return;

        LastPickerIndex = ActivePlayerIndex;

        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);

        CurrentClue = null;
        ActivePlayerIndex = null;

        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
        OnPropertyChanged(nameof(LastPickerIndex));
    }
    //checks if active player's answer is correct or not,
    //adjusts their score, saves clue state, and returns to board view
    public async Task ResolveCurrentClueAsync(bool isCorrect)
    {
        if (CurrentClue == null)
            return;
        //cant deduct points if theres no active player
        if (ActivePlayerIndex == null)
            throw new InvalidOperationException("A player needs to buzz in before scoring this clue.");

        Player player = Players[ActivePlayerIndex.Value];
        //add points if correct, opposite if not
        player.Score += isCorrect
            ? CurrentClue.PointValue
            : -CurrentClue.PointValue;
        //track who made a move
        LastPickerIndex = ActivePlayerIndex;
        //mark clue as completed in the db
        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);
        //reset active clue state
        CurrentClue = null;
        ActivePlayerIndex = null;

        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
        OnPropertyChanged(nameof(LastPickerIndex));
    }
    //checks if game is over by checking every clue on the board
    public bool IsGameComplete()
    {
        return Categories != null &&
               Categories.SelectMany(category => category.Clues).All(clue => clue.IsCompleted);
    }
    //returns list of players from highest to lowest
    public List<Player> GetRankedPlayers()
    {
        return Players.OrderByDescending(player => player.Score).ToList();
    }
    //converts scores into currencies
    public static string FormatScore(int score)
    {
        return score < 0 ? $"-${Math.Abs(score)}" : $"${score}";
    }
}