using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba.ViewModels;
//handles background music, and making preset games
public class MainMenuViewModel : BaseViewModel
{
    //services used by main menu to load games and for audio
    private readonly GameDatabaseService _dbService;
    private readonly BgmService _bgmService;
    //private field to track if music is on or off
    private bool _isMusicEnabled;
    
    public MainMenuViewModel(
        GameDatabaseService dbService,
        BgmService bgmService) {
        _dbService = dbService;
        _bgmService = bgmService;
        //sync local property with saved user preference from bgmservice
        IsMusicEnabled = _bgmService.IsEnabled;
        _ = _bgmService.InitializeAsync(); // fire-and-forget, loads + starts music
    }
    //ui binds to display current state of music toggle
    public bool IsMusicEnabled
    {
        get => _isMusicEnabled;
        //automatically triggers onpropertychanged so the ui updates when toggled
        private set => SetProperty(ref _isMusicEnabled, value);
    }
    //ui helper, fetches list of ready to play categories from the database
    public async Task<List<CategoryDb>> GetAvailableCategoriesAsync()
    {
        return await _dbService.GetAvailableCategoriesAsync();
    }
    //builds preset general knowledge game using categories chosen by the user
    public async Task<int> BuildGeneralKnowledgeGameAsync(
        List<string> chosenCategories)
    {
        //uses the database ti build the custom game with default values
        return await _dbService.BuildCustomGameFromCategoriesAsync(
            "General Knowledge",
            chosenCategories);
    }
    //toggles bgm on or off
    public void ToggleMusic()
    {
        IsMusicEnabled = _bgmService.Toggle();
    }
    //local view model
    public void RefreshMusicState()
    {
        IsMusicEnabled = _bgmService.IsEnabled;
    }
}