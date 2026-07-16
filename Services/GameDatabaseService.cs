using SQLite;
using SQLiteNetExtensionsAsync.Extensions;
using System.Text.Json;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Services;

public class GameDatabaseService
{
    private SQLiteAsyncConnection? _database;

    private async Task InitAsync()
    {
        if (_database != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "JeopardyCluebaseV2.db3");
        _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);

        await _database.CreateTableAsync<GameDb>();
        await _database.CreateTableAsync<CategoryDb>();
        await _database.CreateTableAsync<ClueDb>();

        // Only migrate if the database is empty
        var count = await _database.Table<CategoryDb>().CountAsync();
        if (count == 0)
        {
            await MigrateJsonToSqlite();
        }
    }

    private async Task MigrateJsonToSqlite()
    {
        var connection = _database!.GetConnection();
        var masterGame = new GameDb { Name = "Master Library", IsPreset = true };
        connection.Insert(masterGame);

        using var stream = await FileSystem.OpenAppPackageFileAsync("jeopardy_clues.json");
        var clues = await JsonSerializer.DeserializeAsync<List<JeopardyClue>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (clues == null) return;

        connection.RunInTransaction(() =>
        {
            var processedCategories = new Dictionary<string, int>();

            foreach (var item in clues)
            {
                if (string.IsNullOrWhiteSpace(item.Category)) continue;
                string catName = item.Category.Trim().ToUpper();

                if (!processedCategories.ContainsKey(catName))
                {
                    var cat = new CategoryDb { Name = catName, GameId = masterGame.Id };
                    connection.Insert(cat);
                    processedCategories[catName] = cat.Id;
                }

                var clue = new ClueDb
                {
                    CategoryId = processedCategories[catName],
                    Question = item.Question ?? string.Empty,
                    Answer = item.Answer ?? string.Empty,
                    PointValue = ParseValue(item.Value)
                };
                connection.Insert(clue);
            }
        });
    }

    private int ParseValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value.Replace("$", "").Replace(",", ""), out var result) ? result : 0;
    }

    // --- DATA ACCESS METHODS ---

    public async Task<List<CategoryDb>> GetAvailableCategoriesAsync()
    {
        await InitAsync();

        var allClues = await _database!.Table<ClueDb>().ToListAsync();

        var topCategoryIds = allClues
            .GroupBy(c => c.CategoryId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        return await _database.Table<CategoryDb>()
            .Where(c => topCategoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Builds a custom game board. For each chosen category, a fresh CategoryDb row is
    /// created (scoped to the new game) and a random sample of clues is copied into it
    /// with standardized, ascending point values — like a real Jeopardy board — instead
    /// of reusing the original scraped point values or moving the source category.
    /// </summary>
    /// <param name="customTitle">Display name for the new game.</param>
    /// <param name="chosenCategories">Category names to pull clues from (e.g. from the Master Library).</param>
    /// <param name="questionsPerCategory">How many clues each category column should have. Default 5, matching classic Jeopardy.</param>
    /// <param name="startingPointValue">Point value of the first (top) clue in each category. Default 200.</param>
    /// <param name="pointIncrement">Amount added per row going down a category. Default 200 (200, 400, 600, 800, 1000...).</param>
    public async Task<int> BuildCustomGameFromCategoriesAsync(
        string customTitle,
        List<string> chosenCategories,
        int questionsPerCategory = 5,
        int startingPointValue = 200,
        int pointIncrement = 200)
    {
        await InitAsync();

        var game = new GameDb { Name = customTitle ?? "Custom Game" };
        await _database!.InsertAsync(game);

        var rng = new Random();

        foreach (var catName in chosenCategories)
        {
            var sourceCategory = await _database.Table<CategoryDb>().FirstOrDefaultAsync(c => c.Name == catName);
            if (sourceCategory == null) continue;

            var sourceClues = await _database.Table<ClueDb>()
                .Where(c => c.CategoryId == sourceCategory.Id)
                .ToListAsync();

            var usableClues = sourceClues
                .Where(c => !string.IsNullOrWhiteSpace(c.Question) && !string.IsNullOrWhiteSpace(c.Answer))
                .ToList();

            if (usableClues.Count == 0) continue;

            // Create a NEW category row scoped to this game, rather than reassigning
            // the source category's GameId. This keeps the Master Library category
            // (and its full clue pool) intact and reusable for future custom games.
            var newCategory = new CategoryDb { Name = sourceCategory.Name, GameId = game.Id };
            await _database.InsertAsync(newCategory);

            var selectedClues = usableClues
                .OrderBy(_ => rng.Next())
                .Take(questionsPerCategory)
                .ToList();

            for (int i = 0; i < selectedClues.Count; i++)
            {
                var newClue = new ClueDb
                {
                    CategoryId = newCategory.Id,
                    Question = selectedClues[i].Question,
                    Answer = selectedClues[i].Answer,
                    PointValue = startingPointValue + (i * pointIncrement),
                    IsCompleted = false
                };
                await _database.InsertAsync(newClue);
            }
        }

        return game.Id;
    }

    /// <summary>
    /// Builds a game from categories/questions the player typed themselves.
    /// Point values (200, 400, 600, 800, 1000...) are always auto-assigned —
    /// the player never sets scoring directly.
    /// </summary>
    public async Task<int> BuildPlayerAuthoredGameAsync(
        string customTitle,
        List<CustomCategoryInput> categories,
        int startingPointValue = 200,
        int pointIncrement = 200)
    {
        await InitAsync();

        var game = new GameDb { Name = string.IsNullOrWhiteSpace(customTitle) ? "Custom Game" : customTitle };
        await _database!.InsertAsync(game);

        foreach (var catInput in categories)
        {
            if (string.IsNullOrWhiteSpace(catInput.CategoryName) || catInput.Clues.Count == 0) continue;

            var newCategory = new CategoryDb { Name = catInput.CategoryName.Trim().ToUpper(), GameId = game.Id };
            await _database.InsertAsync(newCategory);

            for (int i = 0; i < catInput.Clues.Count; i++)
            {
                var (question, answer) = catInput.Clues[i];
                if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer)) continue;

                var newClue = new ClueDb
                {
                    CategoryId = newCategory.Id,
                    Question = question.Trim(),
                    Answer = answer.Trim(),
                    PointValue = startingPointValue + (i * pointIncrement),
                    IsCompleted = false
                };
                await _database.InsertAsync(newClue);
            }
        }

        return game.Id;
    }

    // methoid added by ara
    public async Task UpdatePlayerAuthoredGameAsync(
        int gameId,
        string gameTitle,
        List<CustomCategoryInput> categories,
        int startingPointValue = 100,
        int pointIncrement = 100)
    {
        await InitAsync();

        var game = await _database!
            .Table<GameDb>()
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            throw new InvalidOperationException("The saved game could not be found.");

        if (game.IsPreset)
            throw new InvalidOperationException("Preset games cannot be edited.");

        game.Name = string.IsNullOrWhiteSpace(gameTitle)
            ? "Custom Game"
            : gameTitle.Trim();

        await _database.UpdateAsync(game);

        var existingCategories = await _database
            .Table<CategoryDb>()
            .Where(c => c.GameId == gameId)
            .OrderBy(c => c.Id)
            .ToListAsync();

        // Editing currently expects the normal 6-category custom board.
        if (existingCategories.Count != categories.Count)
        {
            throw new InvalidOperationException(
                "The saved board structure does not match the editor.");
        }

        for (int column = 0; column < categories.Count; column++)
        {
            CustomCategoryInput inputCategory = categories[column];
            CategoryDb databaseCategory = existingCategories[column];

            databaseCategory.Name =
                inputCategory.CategoryName.Trim().ToUpper();

            await _database.UpdateAsync(databaseCategory);

            var existingClues = await _database
                .Table<ClueDb>()
                .Where(c => c.CategoryId == databaseCategory.Id)
                .OrderBy(c => c.PointValue)
                .ToListAsync();

            if (existingClues.Count != inputCategory.Clues.Count)
            {
                throw new InvalidOperationException(
                    $"Category {column + 1} does not contain the expected number of clues.");
            }

            for (int row = 0; row < inputCategory.Clues.Count; row++)
            {
                var (question, answer) = inputCategory.Clues[row];

                ClueDb databaseClue = existingClues[row];

                databaseClue.Question = question.Trim();
                databaseClue.Answer = answer.Trim();

                databaseClue.PointValue =
                    startingPointValue + (row * pointIncrement);

                // Editing the wording should not preserve a completed gameplay state.
                databaseClue.IsCompleted = false;

                await _database.UpdateAsync(databaseClue);
            }
        }
    }

    public async Task<List<GameDb>> GetAllGamesAsync()
    {
        await InitAsync();
        return await _database!.Table<GameDb>().ToListAsync();
    }

    public async Task<GameDb> GetGameWithDetailsAsync(int gameId)
    {
        await InitAsync();
        return await _database!.GetWithChildrenAsync<GameDb>(gameId, recursive: true);
    }
    //METHOD ADDED BY VINCE, DELETE FUNCTION FOR THE SAVED GAMES
    public async Task DeleteGameAsync(int gameId)
    {
        await InitAsync();
        var categories = await _database!.Table<CategoryDb>().Where(category=> category.GameId == gameId).ToListAsync();
        foreach (var category in categories)
        {
            var clues = await _database!.Table<ClueDb>().Where(clue => clue.CategoryId == category.Id).ToListAsync();
            foreach (var clue in clues)
            {
                await _database.DeleteAsync(clue);
            }
            await _database.DeleteAsync(category);
        }
        var game = await _database.Table<GameDb>().FirstOrDefaultAsync(g => g.Id == gameId);
        if (game != null)
        { await _database.DeleteAsync(game); }
    }

    public async Task UpdateClueStateAsync(ClueDb clue)
    {
        await InitAsync();
        await _database!.UpdateAsync(clue);
    }

    public async Task UpdateGameNameAsync(int gameId, string newName)
    {
        await InitAsync();
        var game = await _database!.Table<GameDb>().FirstOrDefaultAsync(g => g.Id == gameId);
        if (game != null)
        {
            game.Name = newName;
            await _database.UpdateAsync(game);
        }
    }
}