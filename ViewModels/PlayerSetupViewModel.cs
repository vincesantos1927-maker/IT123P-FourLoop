using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;

public class PlayerSetupViewModel : BaseViewModel
{
    private readonly GameDatabaseService _dbService;
    private readonly PlayerSetupService _setupService;

    private string _boardName = string.Empty;

    public PlayerSetupViewModel(
        GameDatabaseService dbService,
        PlayerSetupService setupService)
    {
        _dbService = dbService;
        _setupService = setupService;

        PlayerCount = _setupService.PlayerCount;
        TimerSeconds = _setupService.TimerSeconds;
    }

    public int PlayerCount { get; private set; }

    public int TimerSeconds { get; private set; }

    public string BoardName
    {
        get => _boardName;
        set => SetProperty(ref _boardName, value);
    }

    public async Task LoadDefaultBoardNameAsync()
    {
        var allGames = await _dbService.GetAllGamesAsync();
        int gameCount = allGames?.Count ?? 0;

        BoardName = $"Board {gameCount + 1}";
    }

    public void IncreasePlayerCount()
    {
        PlayerCount = _setupService.IncrementPlayerCount();
        OnPropertyChanged(nameof(PlayerCount));
    }

    public void DecreasePlayerCount()
    {
        PlayerCount = _setupService.DecrementPlayerCount();
        OnPropertyChanged(nameof(PlayerCount));
    }

    public void IncreaseTimer()
    {
        TimerSeconds = _setupService.IncrementTimer();
        OnPropertyChanged(nameof(TimerSeconds));
    }

    public void DecreaseTimer()
    {
        TimerSeconds = _setupService.DecrementTimer();
        OnPropertyChanged(nameof(TimerSeconds));
    }

    public List<Player> CreatePlayers(IReadOnlyList<string> playerNames)
    {
        return _setupService.CreatePlayers(playerNames);
    }
}