using System.Text.Json;
using JeoAnoBa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Data;

public static class DataSeeder
{
    public static async Task SeedMasterLibraryAsync(JeopardyDbContext db, IWebHostEnvironment env)
    {
        bool alreadySeeded = await db.Categories.AnyAsync();
        if (alreadySeeded) return;

        string jsonPath = Path.Combine(env.ContentRootPath, "Data", "jeopardy_clues.json");
        if (!File.Exists(jsonPath)) return;

        using var stream = File.OpenRead(jsonPath);
        var clues = await JsonSerializer.DeserializeAsync<List<JeopardyClueSeed>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (clues == null) return;

        var masterGame = new GameDb { Name = "Master Library", IsPreset = true };
        db.Games.Add(masterGame);
        await db.SaveChangesAsync();

        var categoryLookup = new Dictionary<string, CategoryDb>();

        foreach (var item in clues)
        {
            if (string.IsNullOrWhiteSpace(item.Category)) continue;
            string catName = item.Category.Trim().ToUpper();

            if (!categoryLookup.TryGetValue(catName, out var category))
            {
                category = new CategoryDb { Name = catName, GameId = masterGame.Id };
                db.Categories.Add(category);
                categoryLookup[catName] = category;
            }

            db.Clues.Add(new ClueDb
            {
                Category = category,
                Question = item.Question ?? string.Empty,
                Answer = item.Answer ?? string.Empty,
                PointValue = ParseValue(item.Value)
            });
        }

        await db.SaveChangesAsync();
    }

    private static int ParseValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value.Replace("$", "").Replace(",", ""), out var result) ? result : 0;
    }
}