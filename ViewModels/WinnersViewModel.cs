using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;

public class WinnersViewModel : BaseViewModel
{
    private readonly GameDatabaseService _dbService;

    public WinnersViewModel(GameDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<List<GameResultDb>> LoadLeaderboardAsync(int take = 10)
    {
        return await _dbService.GetLeaderboardAsync(take);
    }
}