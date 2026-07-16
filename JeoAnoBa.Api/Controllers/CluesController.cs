using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace JeoAnoBa.Api.Controllers;

// Handles requests to "api/Clues"
[ApiController]
[Route("api/[controller]")]
public class CluesController : ControllerBase
{
    // Database context injected automatically, gives access to the Clues table
    private readonly JeopardyDbContext _db;

    public CluesController(JeopardyDbContext db)
    {
        _db = db;
    }

    // Handles PUT requests to "api/Clues/{id}" — e.g. "api/Clues/12"
    // Updates an existing clue's question, answer, points, and completed status.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClue(int id, ClueUpdateRequest request)
    {
        // Look up the clue by its primary key
        var clue = await _db.Clues.FindAsync(id);
        // If no clue with that id exists, respond with 404 Not Found
        if (clue == null)
            return NotFound();

        // Overwrite the existing clue's fields with the new values from the request
        clue.Question = request.Question;
        clue.Answer = request.Answer;
        clue.PointValue = request.PointValue;
        clue.IsCompleted = request.IsCompleted;

        // Save the change to the database
        await _db.SaveChangesAsync();
        return NoContent();
    }
}