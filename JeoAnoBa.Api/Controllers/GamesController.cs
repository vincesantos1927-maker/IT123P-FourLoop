using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Controllers;

// Handles requests to "api/Games" — creating, reading, renaming, deleting, and building games
[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    // Database context injected automatically, gives access to Games/Categories/Clues tables
    private readonly JeopardyDbContext _db;

    public GamesController(JeopardyDbContext db)
    {
        _db = db;
    }

    // GET /api/games
    // Returns every saved game (presets and custom), without their categories/clues loaded
    [HttpGet]
    public async Task<ActionResult<List<GameDb>>> GetAllGames()
    {
        var games = await _db.Games.ToListAsync();
        return Ok(games);
    }

    // GET /api/games/5
    // Returns one specific game, fully loaded with its categories AND each category's clues
    [HttpGet("{id}")]
    public async Task<ActionResult<GameDb>> GetGameWithDetails(int id)
    {
        var game = await _db.Games
            .Include(g => g.Categories)
                .ThenInclude(c => c.Clues)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game == null)
            return NotFound();

        return Ok(game);
    }

    // DELETE /api/games/5
    // Deletes a game by id. Because of the cascade delete rule set up in JeopardyDbContext,
    // removing the game also automatically removes its categories and their clues.
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGame(int id)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null)
            return NotFound();

        _db.Games.Remove(game);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // PUT /api/games/5/name
    // Renames an existing game. Only the Name field is changed.
    [HttpPut("{id}/name")]
    public async Task<IActionResult> RenameGame(int id, RenameGameRequest request)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null)
            return NotFound();

        game.Name = request.Name;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // POST /api/games/from-categories
     // Builds a brand new game by pulling existing clues from already-seeded categories
    // (e.g. from the "Master Library"), rather than the player writing new questions.
    [HttpPost("from-categories")]
    public async Task<ActionResult<int>> BuildFromCategories(BuildFromCategoriesRequest request)
    {
        // Create the new game shell first, saving immediately so it gets a real Id
        var game = new GameDb { Name = string.IsNullOrWhiteSpace(request.CustomTitle) ? "Custom Game" : request.CustomTitle };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        var rng = new Random();

        // Go through each category name the player picked
        foreach (var catName in request.ChosenCategories)
        {
            // Find the existing source category with that name
            var sourceCategory = await _db.Categories.FirstOrDefaultAsync(c => c.Name == catName);
            if (sourceCategory == null) continue;

            // Grab all clues from that source category that actually have real question/answer text
            var usableClues = await _db.Clues
                .Where(c => c.CategoryId == sourceCategory.Id
                            && c.Question != "" && c.Answer != "")
                .ToListAsync();

            if (usableClues.Count == 0) continue; // skip if there's nothing usable in this category

            // Create a fresh category row under the NEW game (copy of the name, not reusing the old row)
            var newCategory = new CategoryDb { Name = sourceCategory.Name, GameId = game.Id };
            _db.Categories.Add(newCategory);
            await _db.SaveChangesAsync();

            // Shuffle the usable clues randomly, then take only as many as requested per category
            var selectedClues = usableClues
                .OrderBy(_ => rng.Next())
                .Take(request.QuestionsPerCategory)
                .ToList();

            // Create new clue rows under the new category
            for (int i = 0; i < selectedClues.Count; i++)
            {
                _db.Clues.Add(new ClueDb
                {
                    CategoryId = newCategory.Id,
                    Question = selectedClues[i].Question,
                    Answer = selectedClues[i].Answer,
                    PointValue = request.StartingPointValue + (i * request.PointIncrement),
                    IsCompleted = false
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(game.Id); // send back the new game's id so the client can load/navigate to it
    }

    // POST /api/games/player-authored
    // Builds a brand new game entirely from questions/answers the player typed themselves
    [HttpPost("player-authored")]
    public async Task<ActionResult<int>> CreatePlayerAuthoredGame(PlayerAuthoredGameRequest request)
    {
        // Create the new game shell first, saving immediately so it gets a real Id
        var game = new GameDb { Name = string.IsNullOrWhiteSpace(request.CustomTitle) ? "Custom Game" : request.CustomTitle };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        // Go through each category the player authored
        foreach (var catInput in request.Categories)
        {
            // Skip categories with no name or no clues at all
            if (string.IsNullOrWhiteSpace(catInput.CategoryName) || catInput.Clues.Count == 0) continue;

            // Create the category row, normalizing the name (trim + uppercase)
            var newCategory = new CategoryDb { Name = catInput.CategoryName.Trim().ToUpper(), GameId = game.Id };
            _db.Categories.Add(newCategory);
            await _db.SaveChangesAsync();

            // Create each clue under this category, skipping any with missing question/answer text
            for (int i = 0; i < catInput.Clues.Count; i++)
            {
                var clueInput = catInput.Clues[i];
                if (string.IsNullOrWhiteSpace(clueInput.Question) || string.IsNullOrWhiteSpace(clueInput.Answer)) continue;

                // Point value scales based on position in the list (row 0, 1, 2...)
                _db.Clues.Add(new ClueDb
                {
                    CategoryId = newCategory.Id,
                    Question = clueInput.Question.Trim(),
                    Answer = clueInput.Answer.Trim(),
                    PointValue = request.StartingPointValue + (i * request.PointIncrement),
                    IsCompleted = false
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(game.Id);
    }

    // PUT /api/games/5/player-authored
    // Edits an EXISTING player-authored game in place (e.g. from edit screen),
    [HttpPut("{id}/player-authored")]
    public async Task<IActionResult> UpdatePlayerAuthoredGame(int id, PlayerAuthoredGameRequest request)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null)
            return NotFound("The saved game could not be found.");

        // Preset (built-in) games are read-only — block any attempt to edit them
        if (game.IsPreset)
            return BadRequest("Preset games cannot be edited.");

        // Update the game's title
        game.Name = string.IsNullOrWhiteSpace(request.CustomTitle) ? "Custom Game" : request.CustomTitle.Trim();

        // Load the game's existing categories in a consistent order (by Id) so they can be
        // matched up positionally with the categories submitted in the request
        var existingCategories = await _db.Categories
            .Where(c => c.GameId == id)
            .OrderBy(c => c.Id)
            .ToListAsync();

        // The edit only works if the submitted board has the same number of categories
        // as what's already saved — otherwise the positions wouldn't line up correctly
        if (existingCategories.Count != request.Categories.Count)
            return BadRequest("The saved board structure does not match the editor.");

        // Walk through each category "column" by position, updating the matching existing row
        for (int column = 0; column < request.Categories.Count; column++)
        {
            var inputCategory = request.Categories[column];
            var databaseCategory = existingCategories[column];

            // Update the category name
            databaseCategory.Name = inputCategory.CategoryName.Trim().ToUpper();

            // Load this category's existing clues in point order, to match them up
            // positionally with the clues submitted for this category
            var existingClues = await _db.Clues
                .Where(c => c.CategoryId == databaseCategory.Id)
                .OrderBy(c => c.PointValue)
                .ToListAsync();

            // Same safety check — clue counts must match to line up correctly
            if (existingClues.Count != inputCategory.Clues.Count)
                return BadRequest($"Category {column + 1} does not contain the expected number of clues.");

            // Update each existing clue's text and recalculate its point value based on position,
            // resetting IsCompleted to false since the content has changed
            for (int row = 0; row < inputCategory.Clues.Count; row++)
            {
                var clueInput = inputCategory.Clues[row];
                var databaseClue = existingClues[row];

                databaseClue.Question = clueInput.Question.Trim();
                databaseClue.Answer = clueInput.Answer.Trim();
                databaseClue.PointValue = request.StartingPointValue + (row * request.PointIncrement);
                databaseClue.IsCompleted = false;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}