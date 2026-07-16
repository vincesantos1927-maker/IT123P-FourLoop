using System.ComponentModel;

namespace jeo_ano_ba.Models;

public class Player : INotifyPropertyChanged // tracks player name and score
{
    public string Name { get; set; } = string.Empty;

    private int _score; // INotifyPropertyChanged to update the UI when score changes
    public int Score
    {
        get => _score;
        set { _score = value; OnPropertyChanged(nameof(Score)); }
    }

    private bool _isActive; // INotifyPropertyChanged to update the UI when active player changes via buzzer
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}