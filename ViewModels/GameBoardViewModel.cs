using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;

public class GameBoardViewModel : BaseViewModel
{
    private readonly GameDatabaseService _dbService;

    public GameBoardViewModel(GameDatabaseService dbService, List<Player> players, int timerSeconds)
    {
        _dbService = dbService;
        Players = players.Count > 0 ? players : new List<Player> { new Player { Name = "Player 1" } };
        TimerSeconds = timerSeconds;
    }

    public List<Player> Players { get; }

    public List<CategoryDb>? Categories { get; private set; }

    public int TimerSeconds { get; }

    public ClueDb? CurrentClue { get; private set; }

    public int? ActivePlayerIndex { get; private set; }

    public int? LastPickerIndex { get; private set; }

    public async Task LoadGameAsync(int gameId)
    {
        GameDb game = await _dbService.GetGameWithDetailsAsync(gameId);
        Categories = game.Categories;
        OnPropertyChanged(nameof(Categories));
    }

    public void SelectClue(ClueDb clue)
    {
        CurrentClue = clue;
        ActivePlayerIndex = null;
        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
    }

    public void BuzzIn(int playerIndex)
    {
        if (CurrentClue == null || ActivePlayerIndex != null)
            return;

        ActivePlayerIndex = playerIndex;
        OnPropertyChanged(nameof(ActivePlayerIndex));
    }

    public async Task SkipCurrentClueAsync()
    {
        if (CurrentClue == null)
            return;

        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);

        CurrentClue = null;
        ActivePlayerIndex = null;

        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
    }
    public async Task ApplyTimeoutPenaltyAsync()
    {
        if (CurrentClue == null || ActivePlayerIndex == null)
            return;

        Player player = Players[ActivePlayerIndex.Value];
        player.Score -= CurrentClue.PointValue;

        LastPickerIndex = ActivePlayerIndex;

        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);

        OnPropertyChanged(nameof(LastPickerIndex));
    }

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

    public async Task ResolveCurrentClueAsync(bool isCorrect)
    {
        if (CurrentClue == null)
            return;

        if (ActivePlayerIndex == null)
            throw new InvalidOperationException("A player needs to buzz in before scoring this clue.");

        Player player = Players[ActivePlayerIndex.Value];

        player.Score += isCorrect
            ? CurrentClue.PointValue
            : -CurrentClue.PointValue;

        LastPickerIndex = ActivePlayerIndex;

        CurrentClue.IsCompleted = true;
        await _dbService.UpdateClueStateAsync(CurrentClue);

        CurrentClue = null;
        ActivePlayerIndex = null;

        OnPropertyChanged(nameof(CurrentClue));
        OnPropertyChanged(nameof(ActivePlayerIndex));
        OnPropertyChanged(nameof(LastPickerIndex));
    }

    public bool IsGameComplete()
    {
        return Categories != null &&
               Categories.SelectMany(category => category.Clues).All(clue => clue.IsCompleted);
    }

    public List<Player> GetRankedPlayers()
    {
        return Players.OrderByDescending(player => player.Score).ToList();
    }

    public static string FormatScore(int score)
    {
        return score < 0 ? $"-${Math.Abs(score)}" : $"${score}";
    }
}