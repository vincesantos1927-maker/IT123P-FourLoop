namespace jeo_ano_ba.Models;

public class SavedGame
{
    public string GameName { get; set; } = string.Empty;

    public string DateCreated { get; set; } = string.Empty;

    public string ProgressText { get; set; } = string.Empty;

    public double ProgressValue { get; set; }
}