using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Views;

public partial class PlayerSetupPage : ContentPage {
    private readonly GameDatabaseService _dbService;
    private readonly int _gameId;
    private readonly PlayerSetupViewModel _viewModel;

    private readonly List<Entry> _playerEntries = new();

    // Assigned to players in order, up to 4 players
    private readonly Color[] _playerColors =
    {
        Color.FromArgb("#FF5252"), // Red
        Color.FromArgb("#4FC3F7"), // Blue
        Color.FromArgb("#4CAF50"), // Green
        Color.FromArgb("#BA68C8")  // Purple
    };

    public PlayerSetupPage(GameDatabaseService dbService, PlayerSetupViewModel viewModel, int gameId) {
        InitializeComponent();

        _dbService = dbService;
        _gameId = gameId;

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        PlayerCountLabel.Text = _viewModel.PlayerCount.ToString();
        TimerLabel.Text = _viewModel.TimerSeconds.ToString();

        BuildPlayers();
        LoadDefaultBoardName();
    }

    // Rebuilds player rows when count changes, updates the timer label when it changes
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        switch (e.PropertyName) {
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
    private async void LoadDefaultBoardName() {
        await _viewModel.LoadDefaultBoardNameAsync(_gameId);
        BoardNameEntry.Text = _viewModel.BoardName;
    }

    // Rebuilds the player name rows (color circle + text entry) to match PlayerCount
    private void BuildPlayers() {
        PlayersContainer.Children.Clear();
        _playerEntries.Clear();

        for (int i = 0; i < _viewModel.PlayerCount; i++) {
            var colorCircle = new Border {
                WidthRequest = 22,
                HeightRequest = 22,
                BackgroundColor = _playerColors[i],
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new RoundRectangle {
                    CornerRadius = new CornerRadius(11)
                }
            };

            var entry = new Entry {
                Placeholder = $"Player {i + 1}",
                PlaceholderColor = Color.FromArgb("#6D7B93"),
                TextColor = Colors.White,
                FontSize = 16,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                ClearButtonVisibility = ClearButtonVisibility.Never
            };

            _playerEntries.Add(entry);

            var entryBorder = new Border {
                BackgroundColor = Color.FromArgb("#0F2342"),
                Stroke = Color.FromArgb("#375D99"),
                StrokeThickness = 1,
                Padding = new Thickness(14, 6),
                HorizontalOptions = LayoutOptions.FillAndExpand,
                StrokeShape = new RoundRectangle {
                    CornerRadius = new CornerRadius(14)
                },
                Content = entry
            };

            var row = new Grid {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 34 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };

            row.Add(colorCircle);
            row.Add(entryBorder, 1);

            PlayersContainer.Children.Add(row);
        }
    }

    // Player count stepper — actual clamping/limits live in the ViewModel
    private void PlayerMinusTapped(object sender, TappedEventArgs e) {
        _viewModel.DecreasePlayerCount();
    }

    private void PlayerPlusTapped(object sender, TappedEventArgs e) {
        _viewModel.IncreasePlayerCount();
    }

    // Timer stepper — actual clamping/limits live in the ViewModel
    private void TimerMinusTapped(object sender, TappedEventArgs e) {
        _viewModel.DecreaseTimer();
    }

    private void TimerPlusTapped(object sender, TappedEventArgs e) {
        _viewModel.IncreaseTimer();
    }

    // Leaves without starting the game
    private async void CloseTapped(object sender, TappedEventArgs e) {
        await Navigation.PopAsync();
    }

    // Validates board name, fills in default names for blank player entries,
    // then starts the game with the configured players/timer/board
    private async void StartGameTapped(object sender, TappedEventArgs e) {
        string boardName = BoardNameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(boardName)) {
            await DisplayAlert("Wait", "Board Name cannot be empty.", "OK");
            return;
        }

        var playerNames = new List<string>();

        for (int i = 0; i < _playerEntries.Count; i++) {
            string name = string.IsNullOrWhiteSpace(_playerEntries[i].Text)
                ? $"Player {i + 1}"
                : _playerEntries[i].Text!.Trim();

            playerNames.Add(name);
        }

        var players = _viewModel.CreatePlayers(playerNames);

        await Navigation.PushAsync(
            new MainPage(
                _dbService,
                players,
                _gameId,
                _viewModel.TimerSeconds,
                boardName,
                new GameTimerService()));
    }
}