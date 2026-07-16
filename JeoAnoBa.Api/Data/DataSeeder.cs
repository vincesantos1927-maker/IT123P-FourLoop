using System.Text.Json;
using JeoAnoBa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Data;

public static class DataSeeder
{
    public static async Task SeedMasterLibraryAsync(JeopardyDbContext db, IWebHostEnvironment env)
    {
        // If categories already exist, seeding already happened before — skip to avoid duplicating data
        bool alreadySeeded = await db.Categories.AnyAsync();
        if (alreadySeeded) return;

        // Build the path to the trivia JSON file and bail out quietly if it's missing
        string jsonPath = Path.Combine(env.ContentRootPath, "Data", "jeopardy_clues.json");
        if (!File.Exists(jsonPath)) return;

        // Read the file and deserialize it into a list of JeopardyClueSeed objects
        // PropertyNameCaseInsensitive means JSON keys don't need exact-case matches to C# property names
        using var stream = File.OpenRead(jsonPath);
        var clues = await JsonSerializer.DeserializeAsync<List<JeopardyClueSeed>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (clues == null) return;

        // Create one parent "Master Library" game to hold all the imported clues, and save it
        // right away so it gets a real Id that the categories below can reference
        var masterGame = new GameDb { Name = "Master Library", IsPreset = true };
        db.Games.Add(masterGame);
        await db.SaveChangesAsync();

        // Cache of categories already created, keyed by name, so duplicate clues
        // in the same category don't each create a separate CategoryDb row
        var categoryLookup = new Dictionary<string, CategoryDb>();

        foreach (var item in clues)
        {
            // Skip any entry with no category name
            if (string.IsNullOrWhiteSpace(item.Category)) continue;

            // Normalize the category name (trim + uppercase) so variations
            string catName = item.Category.Trim().ToUpper();

            // Reuse the existing category if we've already created it this run;
            // otherwise create a new one and add it to the cache
            if (!categoryLookup.TryGetValue(catName, out var category))
            {
                category = new CategoryDb { Name = catName, GameId = masterGame.Id };
                db.Categories.Add(category);
                categoryLookup[catName] = category;
            }

            // Create the clue itself, linking it to its category object directly
            // (EF Core fills in the foreign key automatically from this reference)
            // and converting the dollar-string value into a real point number
            db.Clues.Add(new ClueDb
            {
                Category = category,
                Question = item.Question ?? string.Empty,
                Answer = item.Answer ?? string.Empty,
                PointValue = ParseValue(item.Value)
            });
        }

        // Save all new categories and clues in a single batch, rather than one save per clue
        await db.SaveChangesAsync();
    }

    // Strips "$" and "," from the raw value string (e.g. "$1,000") and parses what's left into an int;
    // returns 0 if the string is empty or can't be parsed
    private static int ParseValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value.Replace("$", "").Replace(",", ""), out var result) ? result : 0;
    }
}