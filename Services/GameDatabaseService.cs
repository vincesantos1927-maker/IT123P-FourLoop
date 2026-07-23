using System.Net;
using System.Net.Http.Json;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Services;
//service communicating with the game server's api, it handles downloading,
//creating, updating, and deleting games, categories, and clues from the database.
public class GameDatabaseService
{
    private readonly HttpClient _httpClient;
    //used to send qequest tothe api server

    //constructor that takes an HttpClient as a parameter, which is used to send requests to the API server.
    public GameDatabaseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    //method that retrieves a list of available categories from the API server.
    public async Task<List<CategoryDb>> GetAvailableCategoriesAsync()
    {
        //sends a GET request to the "api/categories/available" endpoint of the API server and returns a list of CategoryDb objects.
        var response = await _httpClient.GetAsync("api/categories/available");
        //stops if failure
        response.EnsureSuccessStatusCode();
        /*reads the response content as a JSON array and deserializes it into a list of CategoryDb objects. 
         * If the response content is null, it returns an empty list.*/
        return await response.Content.ReadFromJsonAsync<List<CategoryDb>>() ?? new List<CategoryDb>();
    }
    //creates a custom game from the chosen categories and sends a POST request to the API server with the game details.
    public async Task<int> BuildCustomGameFromCategoriesAsync(
        string customTitle,
        List<string> chosenCategories,
        int questionsPerCategory = 5,
        int startingPointValue = 200,
        int pointIncrement = 200)
    {
        //takes the inputs into a single request object to send to the api server
        var request = new BuildFromCategoriesRequest
        {
            CustomTitle = string.IsNullOrWhiteSpace(customTitle) ? "Custom Game" : customTitle,
            ChosenCategories = chosenCategories,
            QuestionsPerCategory = questionsPerCategory,
            StartingPointValue = startingPointValue,
            PointIncrement = pointIncrement
        };
        //sends a POST request to the "api/games/from-categories" endpoint of the API server with the request object as JSON content.
        var response = await _httpClient.PostAsJsonAsync("api/games/from-categories", request);
        response.EnsureSuccessStatusCode();
        //returns the id of the newly created game
        return await response.Content.ReadFromJsonAsync<int>();
    }
    //creates a new custom game
    public async Task<int> BuildPlayerAuthoredGameAsync(
        string customTitle,
        List<CustomCategoryInput> categories,
        int startingPointValue = 200,
        int pointIncrement = 200)
    {
        //inputs into request again
        var request = new PlayerAuthoredGameRequest
        {
            CustomTitle = string.IsNullOrWhiteSpace(customTitle) ? "Custom Game" : customTitle,
            Categories = categories.Select(ToDto).ToList(),
            StartingPointValue = startingPointValue,
            PointIncrement = pointIncrement
        };
        //post the request to the player-authored game builder endpoint
        var response = await _httpClient.PostAsJsonAsync("api/games/player-authored", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }
    //updates an existing custom game
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
        //sends request to update the specific game matching the ID in the url
        var response = await _httpClient.PutAsJsonAsync($"api/games/{gameId}/player-authored", request);
        //game wasnt found error handler
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("The saved game could not be found.");
        //another error handler
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(message) ? "The saved board could not be updated." : message);
        }

        response.EnsureSuccessStatusCode();
    }
    //CustomCategoryInput class becomes CategoryInputDto
    private static CategoryInputDto ToDto(CustomCategoryInput category) => new()
    {
        CategoryName = category.CategoryName,
        //maps each ui question into DTO model
        Clues = category.Clues
            .Select(clue => new ClueInputDto { Question = clue.Question, Answer = clue.Answer })
            .ToList()
    };
    //gets list of saved games
    public async Task<List<GameDb>> GetAllGamesAsync()
    {
        var response = await _httpClient.GetAsync("api/games");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GameDb>>() ?? new List<GameDb>();
    }
    //gets a game with its categories, clues, answrs
    public async Task<GameDb> GetGameWithDetailsAsync(int gameId)
    {
        var response = await _httpClient.GetAsync($"api/games/{gameId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GameDb>()
            ?? throw new InvalidOperationException("The game could not be loaded.");
    }
    //deletes a game from the database using an id
    public async Task DeleteGameAsync(int gameId)
    {
        var response = await _httpClient.DeleteAsync($"api/games/{gameId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }
    //updates the state ofa clue
    public async Task UpdateClueStateAsync(ClueDb clue)
    {
        var request = new ClueUpdateRequest
        {
            Question = clue.Question,
            Answer = clue.Answer,
            PointValue = clue.PointValue,
            IsCompleted = clue.IsCompleted
        };
        //puts the update into specific clue ID
        var response = await _httpClient.PutAsJsonAsync($"api/clues/{clue.Id}", request);
        response.EnsureSuccessStatusCode();
    }
    //renames existing game directly without changing anything
    public async Task UpdateGameNameAsync(int gameId, string newName)
    {
        var request = new RenameGameRequest { Name = newName };
        // sends request to the endpoints
        var response = await _httpClient.PutAsJsonAsync($"api/games/{gameId}/name", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        response.EnsureSuccessStatusCode();
    }
    // records the final scores of a finished game so they show up on the Hall of Fame leaderboard
    public async Task RecordGameResultsAsync(string gameName, List<Player> players)
    {
        var request = new RecordGameResultsRequest
        {
            GameName = string.IsNullOrWhiteSpace(gameName) ? "Custom Game" : gameName,
            Players = players.Select(p => new PlayerResultDto { Name = p.Name, Score = p.Score }).ToList()
        };

        var response = await _httpClient.PostAsJsonAsync("api/results", request);
        response.EnsureSuccessStatusCode();
    }

    // gets the top N highest scores ever recorded, across every game played
    public async Task<List<GameResultDb>> GetLeaderboardAsync(int take = 10)
    {
        var response = await _httpClient.GetAsync($"api/results/leaderboard?take={take}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GameResultDb>>() ?? new List<GameResultDb>();
    }
}