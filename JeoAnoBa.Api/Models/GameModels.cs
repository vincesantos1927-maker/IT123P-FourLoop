using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JeoAnoBa.Api.Models;

public class GameDb
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsPreset { get; set; }

    // EF Core infers this is the "many" side of a one-to-many via CategoryDb.GameId.
    public List<CategoryDb> Categories { get; set; } = new();
}

public class CategoryDb
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Game))]
    public int GameId { get; set; }
    public GameDb? Game { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<ClueDb> Clues { get; set; } = new();
}

public class ClueDb
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }
    public CategoryDb? Category { get; set; }

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int PointValue { get; set; }
    public bool IsCompleted { get; set; }
}
