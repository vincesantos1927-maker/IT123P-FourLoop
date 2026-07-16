using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Controllers;

// [Route("api/[controller]")] means this controller handles requests to "api/Categories"
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    // EF Core database context, injected automatically by ASP.NET Core whenever
    // this controller is created — gives access to Games/Categories/Clues tables
    private readonly JeopardyDbContext _db;

    public CategoriesController(JeopardyDbContext db)
    {
        _db = db;
    }

    // Handles GET requests to "api/Categories/available"
    [HttpGet("available")]
    public async Task<ActionResult<List<CategoryDb>>> GetAvailableCategories()
    {
        // Group all clues by which category they belong to, count how many clues
        // are in each group, sort so the biggest groups come first, then take
        // just the top 10 category IDs (the categories with the most clues)
        var topCategoryIds = await _db.Clues
            .GroupBy(c => c.CategoryId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToListAsync();

        // Fetch the actual category rows matching those top 10 IDs,
        // sorted alphabetically by name for display
        var categories = await _db.Categories
            .Where(c => topCategoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }
}