namespace jeo_ano_ba.Models;

public class ClueInputDto // used to store questions (not yet saved)
{
    public string Question { get; set; } = string.Empty; // user input
    public string Answer { get; set; } = string.Empty; // user input
}

public class CategoryInputDto // used to store categories (not yet saved)
{
    public string CategoryName { get; set; } = string.Empty; // user input
    public List<ClueInputDto> Clues { get; set; } = new();
}

public class PlayerAuthoredGameRequest // used when making a custom game
{
    public string CustomTitle { get; set; } = "Custom Game"; // user input but with placeholder
    public List<CategoryInputDto> Categories { get; set; } = new();
    public int StartingPointValue { get; set; } = 100;
    public int PointIncrement { get; set; } = 100;
}

public class BuildFromCategoriesRequest // used when making a preset game
{
    public string CustomTitle { get; set; } = "Custom Game"; // user input but with placeholder
    public List<string> ChosenCategories { get; set; } = new();
    public int QuestionsPerCategory { get; set; } = 5;
    public int StartingPointValue { get; set; } = 200;
    public int PointIncrement { get; set; } = 200;
}

public class RenameGameRequest // used when renaming a game
{
    public string Name { get; set; } = string.Empty; // user input
}

public class ClueUpdateRequest // used by both modes when updating a clue
{
    public string Question { get; set; } = string.Empty; // user input
    public string Answer { get; set; } = string.Empty; // user input
    public int PointValue { get; set; }
    public bool IsCompleted { get; set; }
}
public class PlayerResultDto
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class RecordGameResultsRequest
{
    public string GameName { get; set; } = "Custom Game";
    public List<PlayerResultDto> Players { get; set; } = new();
}