using System.Net;
using System.Net.Http.Json;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Services;

public class GameDatabaseService
{
    private readonly HttpClient _httpClient;

    public GameDatabaseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CategoryDb>> GetAvailableCategoriesAsync()
    {
        var response = await _httpClient.GetAsync("api/categories/available");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CategoryDb>>() ?? new List<CategoryDb>();
    }

    public async Task<int> BuildCustomGameFromCategoriesAsync(
        string customTitle,
        List<string> chosenCategories,
        int questionsPerCategory = 5,
        int startingPointValue = 200,
        int pointIncrement = 200)
    {
        var request = new BuildFromCategoriesRequest
        {
            CustomTitle = string.IsNullOrWhiteSpace(customTitle) ? "Custom Game" : customTitle,
            ChosenCategories = chosenCategories,
            QuestionsPerCategory = questionsPerCategory,
            StartingPointValue = startingPointValue,
            PointIncrement = pointIncrement
        };

        var response = await _httpClient.PostAsJsonAsync("api/games/from-categories", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<int> BuildPlayerAuthoredGameAsync(
        string customTitle,
        List<CustomCategoryInput> categories,
        int startingPointValue = 200,
        int pointIncrement = 200)
    {
        var request = new PlayerAuthoredGameRequest
        {
            CustomTitle = string.IsNullOrWhiteSpace(customTitle) ? "Custom Game" : customTitle,
            Categories = categories.Select(ToDto).ToList(),
            StartingPointValue = startingPointValue,
            PointIncrement = pointIncrement
        };

        var response = await _httpClient.PostAsJsonAsync("api/games/player-authored", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task UpdatePlayerAuthoredGameAsync(
        int gameId,
        string gameTitle,
        List<CustomCategoryInput> categories,
        int startingPointValue = 100,
        int pointIncrement = 100)
    {
        var request = new PlayerAuthoredGameRequest
        {
            CustomTitle = string.IsNullOrWhiteSpace(gameTitle) ? "Custom Game" : gameTitle.Trim(),
            Categories = categories.Select(ToDto).ToList(),
            StartingPointValue = startingPointValue,
            PointIncrement = pointIncrement
        };

        var response = await _httpClient.PutAsJsonAsync($"api/games/{gameId}/player-authored", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("The saved game could not be found.");

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(message) ? "The saved board could not be updated." : message);
        }

        response.EnsureSuccessStatusCode();
    }

    private static CategoryInputDto ToDto(CustomCategoryInput category) => new()
    {
        CategoryName = category.CategoryName,
        Clues = category.Clues
            .Select(clue => new ClueInputDto { Question = clue.Question, Answer = clue.Answer })
            .ToList()
    };

    public async Task<List<GameDb>> GetAllGamesAsync()
    {
        var response = await _httpClient.GetAsync("api/games");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GameDb>>() ?? new List<GameDb>();
    }

    public async Task<GameDb> GetGameWithDetailsAsync(int gameId)
    {
        var response = await _httpClient.GetAsync($"api/games/{gameId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GameDb>()
            ?? throw new InvalidOperationException("The game could not be loaded.");
    }

    public async Task DeleteGameAsync(int gameId)
    {
        var response = await _httpClient.DeleteAsync($"api/games/{gameId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateClueStateAsync(ClueDb clue)
    {
        var request = new ClueUpdateRequest
        {
            Question = clue.Question,
            Answer = clue.Answer,
            PointValue = clue.PointValue,
            IsCompleted = clue.IsCompleted
        };

        var response = await _httpClient.PutAsJsonAsync($"api/clues/{clue.Id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateGameNameAsync(int gameId, string newName)
    {
        var request = new RenameGameRequest { Name = newName };
        var response = await _httpClient.PutAsJsonAsync($"api/games/{gameId}/name", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }
}