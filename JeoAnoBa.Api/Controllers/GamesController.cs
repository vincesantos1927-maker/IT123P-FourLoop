using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly JeopardyDbContext _db;

    public GamesController(JeopardyDbContext db)
    {
        _db = db;
    }

    // GET /api/games
    [HttpGet]
    public async Task<ActionResult<List<GameDb>>> GetAllGames()
    {
        var games = await _db.Games.ToListAsync();
        return Ok(games);
    }

    // GET /api/games/5
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
    [HttpPost("from-categories")]
    public async Task<ActionResult<int>> BuildFromCategories(BuildFromCategoriesRequest request)
    {
        var game = new GameDb { Name = string.IsNullOrWhiteSpace(request.CustomTitle) ? "Custom Game" : request.CustomTitle };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        var rng = new Random();

        foreach (var catName in request.ChosenCategories)
        {
            var sourceCategory = await _db.Categories.FirstOrDefaultAsync(c => c.Name == catName);
            if (sourceCategory == null) continue;

            var usableClues = await _db.Clues
                .Where(c => c.CategoryId == sourceCategory.Id
                            && c.Question != "" && c.Answer != "")
                .ToListAsync();

            if (usableClues.Count == 0) continue;

            var newCategory = new CategoryDb { Name = sourceCategory.Name, GameId = game.Id };
            _db.Categories.Add(newCategory);
            await _db.SaveChangesAsync();

            var selectedClues = usableClues
                .OrderBy(_ => rng.Next())
                .Take(request.QuestionsPerCategory)
                .ToList();

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
        return Ok(game.Id);
    }

    // POST /api/games/player-authored
    [HttpPost("player-authored")]
    public async Task<ActionResult<int>> CreatePlayerAuthoredGame(PlayerAuthoredGameRequest request)
    {
        var game = new GameDb { Name = string.IsNullOrWhiteSpace(request.CustomTitle) ? "Custom Game" : request.CustomTitle };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        foreach (var catInput in request.Categories)
        {
            if (string.IsNullOrWhiteSpace(catInput.CategoryName) || catInput.Clues.Count == 0) continue;

            var newCategory = new CategoryDb { Name = catInput.CategoryName.Trim().ToUpper(), GameId = game.Id };
            _db.Categories.Add(newCategory);
            await _db.SaveChangesAsync();

            for (int i = 0; i < catInput.Clues.Count; i++)
            {
                var clueInput = catInput.Clues[i];
                if (string.IsNullOrWhiteSpace(clueInput.Question) || string.IsNullOrWhiteSpace(clueInput.Answer)) continue;

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
    [HttpPut("{id}/player-authored")]
    public async Task<IActionResult> UpdatePlayerAuthoredGame(int id, PlayerAuthoredGameRequest request)
    {
        var game = await _db.Games.FindAsync(id);
        if (game == null)
            return NotFound("The saved game could not be found.");

        if (game.IsPreset)
            return BadRequest("Preset games cannot be edited.");

        game.Name = string.IsNullOrWhiteSpace(request.CustomTitle) ? "Custom Game" : request.CustomTitle.Trim();

        var existingCategories = await _db.Categories
            .Where(c => c.GameId == id)
            .OrderBy(c => c.Id)
            .ToListAsync();

        if (existingCategories.Count != request.Categories.Count)
            return BadRequest("The saved board structure does not match the editor.");

        for (int column = 0; column < request.Categories.Count; column++)
        {
            var inputCategory = request.Categories[column];
            var databaseCategory = existingCategories[column];

            databaseCategory.Name = inputCategory.CategoryName.Trim().ToUpper();

            var existingClues = await _db.Clues
                .Where(c => c.CategoryId == databaseCategory.Id)
                .OrderBy(c => c.PointValue)
                .ToListAsync();

            if (existingClues.Count != inputCategory.Clues.Count)
                return BadRequest($"Category {column + 1} does not contain the expected number of clues.");

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