namespace JeoAnoBa.Api.Models;

public class ClueInputDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class CategoryInputDto
{
    public string CategoryName { get; set; } = string.Empty;
    public List<ClueInputDto> Clues { get; set; } = new();
}

public class PlayerAuthoredGameRequest
{
    public string CustomTitle { get; set; } = "Custom Game";
    public List<CategoryInputDto> Categories { get; set; } = new();
    public int StartingPointValue { get; set; } = 100;
    public int PointIncrement { get; set; } = 100;
}

public class BuildFromCategoriesRequest
{
    public string CustomTitle { get; set; } = "Custom Game";
    public List<string> ChosenCategories { get; set; } = new();
    public int QuestionsPerCategory { get; set; } = 5;
    public int StartingPointValue { get; set; } = 200;
    public int PointIncrement { get; set; } = 200;
}

public class RenameGameRequest
{
    public string Name { get; set; } = string.Empty;
}

public class ClueUpdateRequest
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int PointValue { get; set; }
    public bool IsCompleted { get; set; }
}

public class JeopardyClueSeed
{
    public string? Category { get; set; }
    public string? Question { get; set; }
    public string? Value { get; set; }
    public string? Answer { get; set; }
}