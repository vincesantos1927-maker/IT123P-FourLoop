using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.ViewModels;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace jeo_ano_ba.Views;

public partial class PlayerSetupPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private readonly int _gameId;
    private readonly SfxService _sfxService;
    private readonly PlayerSetupViewModel _viewModel;

    private readonly List<Entry> _playerEntries = new();
    private readonly string?[] _photoPaths = new string?[4];
    private readonly Color?[] _chosenColors = new Color?[4];
    private readonly bool[] _colorChosen = new bool[4];

    // per-slot default color, shown until the player picks a photo or cycles a different color
    private static readonly Color[] _playerColors =
    {
        Color.FromArgb("#FF5252"),
        Color.FromArgb("#4FC3F7"),
        Color.FromArgb("#4CAF50"),
        Color.FromArgb("#BA68C8")
    };
    private static readonly Color[] AvatarColorChoices =
    {
        Color.FromArgb("#FF5252"),
        Color.FromArgb("#4FC3F7"),
        Color.FromArgb("#4CAF50"),
        Color.FromArgb("#BA68C8"),
        Color.FromArgb("#FFB74D"),
        Color.FromArgb("#F06292")
    };


    public PlayerSetupPage(GameDatabaseService dbService, SfxService sfxService, PlayerSetupViewModel viewModel, int gameId)
    {
        InitializeComponent();

        _dbService = dbService;
        _sfxService = sfxService;
        _gameId = gameId;

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        PlayerCountLabel.Text = _viewModel.PlayerCount.ToString();
        TimerLabel.Text = _viewModel.TimerSeconds.ToString();

        BuildPlayers();
        LoadDefaultBoardName();
    }

    // Rebuilds player rows when count changes, updates the timer label when it changes
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlayerSetupViewModel.PlayerCount):
                PlayerCountLabel.Text = _viewModel.PlayerCount.ToString();
                BuildPlayers();
                break;
            case nameof(PlayerSetupViewModel.TimerSeconds):
                TimerLabel.Text = _viewModel.TimerSeconds.ToString();
                break;
        }
    }

    // Pre-fills the board name field with the board's saved/default name
    private async void LoadDefaultBoardName()
    {
        await _viewModel.LoadDefaultBoardNameAsync(_gameId);
        BoardNameEntry.Text = _viewModel.BoardName;
    }

    // Rebuilds the player name rows (color circle + text entry) to match PlayerCount
    private void BuildPlayers()
    {
        PlayersContainer.Children.Clear();
        _playerEntries.Clear();

        for (int i = 0; i < _viewModel.PlayerCount; i++)
        {
            var placeholderLabel = new Label
            {
                Text = "📷",
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Colors.White
            };

            var avatarImage = new Image
            {
                Aspect = Aspect.AspectFill,
                WidthRequest = 40,
                HeightRequest = 40,
                IsVisible = _photoPaths[i] != null
            };

            if (_photoPaths[i] != null)
                avatarImage.Source = ImageSource.FromFile(_photoPaths[i]);

            placeholderLabel.IsVisible = _photoPaths[i] == null && !_colorChosen[i];

            var avatarContent = new Grid();
            avatarContent.Children.Add(placeholderLabel);
            avatarContent.Children.Add(avatarImage);

            bool hasPhoto = _photoPaths[i] != null;
            bool hasColor = _colorChosen[i] && !hasPhoto;

            Color neutralFill = Color.FromArgb("#0F2342");
            Color neutralStroke = Color.FromArgb("#375D99");
            Color pickedColor = _chosenColors[i] ?? _playerColors[i];

            Color fillColor = hasPhoto ? Colors.Transparent : (hasColor ? pickedColor : neutralFill);
            Color strokeColor = hasPhoto ? Colors.Transparent : (hasColor ? pickedColor : neutralStroke);

            var avatarBorder = new Border
            {
                WidthRequest = 40,
                HeightRequest = 40,
                BackgroundColor = fillColor,
                Stroke = strokeColor,
                StrokeThickness = hasPhoto ? 0 : (hasColor ? 2 : 1),
                Padding = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
                Content = avatarContent
            };

            int photoIndex = i;
            var avatarTap = new TapGestureRecognizer();
            avatarTap.Tapped += (s, e) => AvatarTapped(photoIndex, avatarImage, placeholderLabel, avatarBorder);
            avatarBorder.GestureRecognizers.Add(avatarTap);

            var entry = new Entry
            {
                Placeholder = $"Player {i + 1}",
                PlaceholderColor = Color.FromArgb("#6D7B93"),
                TextColor = Colors.White,
                FontSize = 16,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                ClearButtonVisibility = ClearButtonVisibility.Never
            };

            _playerEntries.Add(entry);

            var entryBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#0F2342"),
                Stroke = Color.FromArgb("#375D99"),
                StrokeThickness = 1,
                Padding = new Thickness(14, 6),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                Content = entry
            };

            var row = new Grid
            {
                ColumnDefinitions = {
                new ColumnDefinition { Width = 46 },
                new ColumnDefinition { Width = GridLength.Star }
            },
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.Center
            };

            row.Add(avatarBorder, 0);
            row.Add(entryBorder, 1);

            PlayersContainer.Children.Add(row);
        }
    }

    // Cycles that player's avatar color, and clears any chosen photo —
    // a player uses either a photo OR a color, never both
    // Cycles that player's avatar color, skipping any color already used by another
    // player slot, and clears any chosen photo — a player uses either a photo OR a
    // color, never both.
    private void CyclePlayerColor(int index, Border avatarBorder, Image avatarImage, Label placeholderLabel)
    {
        _photoPaths[index] = null;
        avatarImage.Source = null;
        avatarImage.IsVisible = false;
        placeholderLabel.IsVisible = true;

        Color current = _chosenColors[index] ?? _playerColors[index];
        int currentIndex = Array.IndexOf(AvatarColorChoices, current);

        Color next = current;
        for (int attempt = 1; attempt <= AvatarColorChoices.Length; attempt++)
        {
            int candidateIndex = (currentIndex + attempt) % AvatarColorChoices.Length;
            Color candidate = AvatarColorChoices[candidateIndex];

            bool takenByAnother = false;
            for (int p = 0; p < _viewModel.PlayerCount; p++)
            {
                if (p == index) continue;

                // Players who currently show a photo aren't "using" any color slot
                Color otherColor = _photoPaths[p] == null ? (_chosenColors[p] ?? _playerColors[p]) : Colors.Transparent;

                if (otherColor == candidate)
                {
                    takenByAnother = true;
                    break;
                }
            }

            if (!takenByAnother)
            {
                next = candidate;
                break;
            }
        }

        _chosenColors[index] = next;
        _colorChosen[index] = true;
        placeholderLabel.IsVisible = false;

        avatarBorder.BackgroundColor = next;
        avatarBorder.Stroke = next;
        avatarBorder.StrokeThickness = 2;
    }

    // Player count stepper — actual clamping/limits live in the ViewModel
    private void PlayerMinusTapped(object sender, TappedEventArgs e)
    {
        _sfxService.PlayClick();
        _viewModel.DecreasePlayerCount();
    }

    private void PlayerPlusTapped(object sender, TappedEventArgs e)
    {
        _sfxService.PlayClick();
        _viewModel.IncreasePlayerCount();
    }

    // Timer stepper — actual clamping/limits live in the ViewModel
    private void TimerMinusTapped(object sender, TappedEventArgs e)
    {
        _sfxService.PlayClick();
        _viewModel.DecreaseTimer();
    }

    private void TimerPlusTapped(object sender, TappedEventArgs e)
    {
        _sfxService.PlayClick();
        _viewModel.IncreaseTimer();
    }

    // Leaves without starting the game
    private async void CloseTapped(object sender, TappedEventArgs e)
    {
        _sfxService.PlayClick();
        await Navigation.PopAsync();
    }

    // Validates board name, fills in default names for blank player entries,
    // then starts the game with the configured players/timer/board
    private async void StartGameTapped(object sender, TappedEventArgs e)
    {
        _sfxService.PlayClick();

        string boardName = BoardNameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(boardName))
        {
            await DisplayAlert("Wait", "Board Name cannot be empty.", "OK");
            return;
        }

        var playerNames = new List<string>();

        for (int i = 0; i < _playerEntries.Count; i++)
        {
            string name = string.IsNullOrWhiteSpace(_playerEntries[i].Text)
                ? $"Player {i + 1}"
                : _playerEntries[i].Text!.Trim();

            playerNames.Add(name);
        }

        var players = _viewModel.CreatePlayers(playerNames);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].PhotoPath = _photoPaths[i];
            players[i].AvatarColor = _chosenColors[i] ?? _playerColors[i];
        }

        await Navigation.PushAsync(
            new MainPage(
                _dbService,
                players,
                _gameId,
                _viewModel.TimerSeconds,
                boardName,
                new GameTimerService()));
    }
    private async void AvatarTapped(int index, Image avatarImage, Label placeholderLabel, Border avatarBorder)
    {
        _sfxService.PlayClick();

        string action = await DisplayActionSheet(
            "Profile Picture", "Cancel", null, "Take Photo", "Choose from Gallery", "Use a Color Instead");

        if (action == "Use a Color Instead")
        {
            CyclePlayerColor(index, avatarBorder, avatarImage, placeholderLabel);
            return;
        }

        FileResult? photo = null;

        try
        {
            if (action == "Take Photo" && MediaPicker.Default.IsCaptureSupported)
            {
                photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else if (action == "Choose from Gallery")
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Not Supported", "This device doesn't support that option.", "OK");
            return;
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permission Needed", "Camera/Gallery permission was denied.", "OK");
            return;
        }

        if (photo == null)
            return;

        string localPath = System.IO.Path.Combine(
            FileSystem.CacheDirectory,
            $"player_{index}_{Guid.NewGuid()}.jpg"
        );

        File.Copy(photo.FullPath, localPath, overwrite: true);

        _photoPaths[index] = localPath;
        _colorChosen[index] = false;

        avatarImage.Source = ImageSource.FromFile(localPath);
        avatarImage.IsVisible = true;
        placeholderLabel.IsVisible = false;
        avatarBorder.BackgroundColor = Colors.Transparent;
        avatarBorder.Stroke = Colors.Transparent;
        avatarBorder.StrokeThickness = 0;
    }

}