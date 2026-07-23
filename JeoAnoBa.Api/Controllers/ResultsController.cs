using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly JeopardyDbContext _db;

    public ResultsController(JeopardyDbContext db)
    {
        _db = db;
    }

    // POST api/results — called once a game finishes, records every player's final score
    [HttpPost]
    public async Task<IActionResult> RecordResults(RecordGameResultsRequest request)
    {
        if (request.Players == null || request.Players.Count == 0)
            return BadRequest("No player results were submitted.");

        int topScore = request.Players.Max(p => p.Score);

        foreach (var player in request.Players)
        {
            _db.GameResults.Add(new GameResultDb
            {
                PlayerName = string.IsNullOrWhiteSpace(player.Name) ? "Player" : player.Name.Trim(),
                Score = player.Score,
                IsWinner = player.Score == topScore,
                GameName = string.IsNullOrWhiteSpace(request.GameName) ? "Custom Game" : request.GameName,
                PlayedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET api/results/leaderboard?take=10 — highest individual scores ever recorded
    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<GameResultDb>>> GetLeaderboard([FromQuery] int take = 10)
    {
        var topResults = await _db.GameResults
            .OrderByDescending(r => r.Score)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync();

        return Ok(topResults);
    }
}