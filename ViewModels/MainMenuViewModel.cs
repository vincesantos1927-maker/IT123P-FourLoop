using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;

public class MainMenuViewModel : BaseViewModel
{
    private readonly GameDatabaseService _dbService;
    private readonly BgmService _bgmService;

    private bool _isMusicEnabled;

    public MainMenuViewModel(
        GameDatabaseService dbService,
        BgmService bgmService) {
        _dbService = dbService;
        _bgmService = bgmService;
        IsMusicEnabled = _bgmService.IsEnabled;
        _ = _bgmService.InitializeAsync(); // fire-and-forget, loads + starts music
    }

    public bool IsMusicEnabled
    {
        get => _isMusicEnabled;
        private set => SetProperty(ref _isMusicEnabled, value);
    }

    public async Task<List<CategoryDb>> GetAvailableCategoriesAsync()
    {
        return await _dbService.GetAvailableCategoriesAsync();
    }

    public async Task<int> BuildGeneralKnowledgeGameAsync(
        List<string> chosenCategories)
    {
        return await _dbService.BuildCustomGameFromCategoriesAsync(
            "General Knowledge",
            chosenCategories);
    }

    public void ToggleMusic()
    {
        IsMusicEnabled = _bgmService.Toggle();
    }
}