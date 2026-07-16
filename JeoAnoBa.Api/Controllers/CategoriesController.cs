using JeoAnoBa.Api.Data;
using JeoAnoBa.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeoAnoBa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly JeopardyDbContext _db;

    public CategoriesController(JeopardyDbContext db)
    {
        _db = db;
    }

    [HttpGet("available")]
    public async Task<ActionResult<List<CategoryDb>>> GetAvailableCategories()
    {
        var topCategoryIds = await _db.Clues
            .GroupBy(c => c.CategoryId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToListAsync();

        var categories = await _db.Categories
            .Where(c => topCategoryIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }
}