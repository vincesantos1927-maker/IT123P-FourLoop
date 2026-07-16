using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace JeoAnoBa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CluesController : ControllerBase
{
    private readonly JeopardyDbContext _db;

    public CluesController(JeopardyDbContext db)
    {
        _db = db;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClue(int id, ClueUpdateRequest request)
    {
        var clue = await _db.Clues.FindAsync(id);
        if (clue == null)
            return NotFound();

        clue.Question = request.Question;
        clue.Answer = request.Answer;
        clue.PointValue = request.PointValue;
        clue.IsCompleted = request.IsCompleted;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}