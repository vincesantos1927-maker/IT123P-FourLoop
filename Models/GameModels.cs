using System.ComponentModel;
using System.Text.Json.Serialization;

namespace jeo_ano_ba.Models;

public class GameDb // whole game board, has info of name mode and list of CategoryDb
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsPreset { get; set; }

    public List<CategoryDb> Categories { get; set; } = new();
}

public class CategoryDb // Belongs to GameDb, holds a list of clueDb 
{
    public int Id { get; set; }

    public int GameId { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<ClueDb> Clues { get; set; } = new();

    public bool IsSelected { get; set; } // for preset games to know which categories are selected
}

public class ClueDb : INotifyPropertyChanged // Holds question and answer for the whole game, belongs to CategoryDb
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int PointValue { get; set; }

    private bool _isCompleted; // has INotifyPropertyChanged to update the UI when a clue is completed
    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            _isCompleted = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
        }
    }

    public string UserIdentificationInput { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class JeopardyClue // used to deserialize the API response 
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("air_date")]
    public string AirDate { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("round")]
    public string Round { get; set; }

    [JsonPropertyName("show_number")]
    public string ShowNumber { get; set; }
}
public class GameResultDb
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsWinner { get; set; }
    public string GameName { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; }
}