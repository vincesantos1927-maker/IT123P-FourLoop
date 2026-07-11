using SQLite;
using SQLiteNetExtensions.Attributes;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace jeo_ano_ba.Models;

[Table("Games")]
public class GameDb
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsPreset { get; set; }

    [OneToMany(CascadeOperations = CascadeOperation.All)]
    public List<CategoryDb> Categories { get; set; } = new();
}

[Table("Categories")]
public class CategoryDb
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ForeignKey(typeof(GameDb))]
    public int GameId { get; set; }

    // 🚀 INDEXED: Searching for categories by name is now instant
    [Indexed]
    public string Name { get; set; } = string.Empty;

    [OneToMany(CascadeOperations = CascadeOperation.All)]
    public List<ClueDb> Clues { get; set; } = new();

    [Ignore]
    public bool IsSelected { get; set; }
}

[Table("Clues")]
public class ClueDb : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // 🚀 INDEXED: Quickly find all clues belonging to a specific category
    [Indexed]
    [ForeignKey(typeof(CategoryDb))]
    public int CategoryId { get; set; }

    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int PointValue { get; set; }

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            _isCompleted = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
        }
    }

    [Ignore]
    public string UserIdentificationInput { get; set; } = string.Empty;

    [Ignore]
    public string CategoryName { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
}
public class JeopardyClue
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("air_date")]
    public string AirDate { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } // Can be null (Final Jeopardy)

    [JsonPropertyName("answer")]
    public string Answer { get; set; }

    [JsonPropertyName("round")]
    public string Round { get; set; }

    [JsonPropertyName("show_number")]
    public string ShowNumber { get; set; }
}
