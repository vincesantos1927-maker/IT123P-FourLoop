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

    //for photos
    private string? _photoPath;
    public string? PhotoPath
    {
        get => _photoPath;
        set
        {
            _photoPath = value;
            OnPropertyChanged(nameof(PhotoPath));
            OnPropertyChanged(nameof(ProfileImageSource));
        }
    }

    // the color to show when no photo is chosen
    private Color? _avatarColor;
    public Color? AvatarColor
    {
        get => _avatarColor;
        set
        {
            _avatarColor = value;
            OnPropertyChanged(nameof(AvatarColor));
        }
    }

    // Bindable image source — falls back to a placeholder if no photo was picked.
    // Add a small "default_avatar.jpg" to Resources/Images if you want a custom fallback icon.
    public ImageSource ProfileImageSource =>
        string.IsNullOrEmpty(PhotoPath)
            ? "default_avatar.jpg"
            : ImageSource.FromFile(PhotoPath);
}