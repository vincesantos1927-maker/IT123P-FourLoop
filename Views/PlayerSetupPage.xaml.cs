using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using Microsoft.Maui.Controls.Shapes;

namespace jeo_ano_ba.Views;

public partial class PlayerSetupPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private readonly int _gameId;

    private int _playerCount = 2;
    private int _timerSeconds = 30;

    private readonly List<Entry> _playerEntries = new();

    private readonly Color[] _playerColors =
    {
        Color.FromArgb("#FF5252"), // Red
        Color.FromArgb("#4FC3F7"), // Blue
        Color.FromArgb("#4CAF50"), // Green
        Color.FromArgb("#BA68C8")  // Purple
    };

    public PlayerSetupPage(GameDatabaseService dbService, int gameId)
    {
        InitializeComponent();

        _dbService = dbService;
        _gameId = gameId;

        PlayerCountLabel.Text = _playerCount.ToString();
        TimerLabel.Text = _timerSeconds.ToString();

        BuildPlayers();
    }

    // ===================================================
    // BUILD PLAYER LIST
    // ===================================================

    private void BuildPlayers()
    {
        PlayersContainer.Children.Clear();
        _playerEntries.Clear();

        for (int i = 0; i < _playerCount; i++)
        {
            var colorCircle = new Border
            {
                WidthRequest = 22,
                HeightRequest = 22,
                BackgroundColor = _playerColors[i],
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(11)
                }
            };

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

            // Rounded textbox container

            var entryBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#0F2342"),

                Stroke = Color.FromArgb("#375D99"),

                StrokeThickness = 1,

                Padding = new Thickness(14, 6),

                HorizontalOptions = LayoutOptions.FillAndExpand,

                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(14)
                },

                Content = entry
            };


            // Player row

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = 34
                    },

                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    }
                },

                ColumnSpacing = 12,

                VerticalOptions = LayoutOptions.Center
            };

            row.Add(colorCircle);

            row.Add(entryBorder, 1);

            PlayersContainer.Children.Add(row);
        }
    }

    // ===================================================
    // PLAYER COUNT
    // ===================================================

    private void PlayerMinusTapped(object sender, TappedEventArgs e)
    {
        if (_playerCount == 2)
            return;

        _playerCount--;

        PlayerCountLabel.Text = _playerCount.ToString();

        BuildPlayers();
    }

    private void PlayerPlusTapped(object sender, TappedEventArgs e)
    {
        if (_playerCount == 4)
            return;

        _playerCount++;

        PlayerCountLabel.Text = _playerCount.ToString();

        BuildPlayers();
    }

    // ===================================================
    // TIMER
    // ===================================================

    private void TimerMinusTapped(object sender, TappedEventArgs e)
    {
        if (_timerSeconds == 5)
            return;

        _timerSeconds -= 5;

        TimerLabel.Text = _timerSeconds.ToString();
    }

    private void TimerPlusTapped(object sender, TappedEventArgs e)
    {
        if (_timerSeconds == 60)
            return;

        _timerSeconds += 5;

        TimerLabel.Text = _timerSeconds.ToString();
    }

    // ===================================================
    // CLOSE
    // ===================================================

    private async void CloseTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    // ===================================================
    // START GAME
    // ===================================================

    private async void StartGameTapped(object sender, TappedEventArgs e)
    {
        var players = new List<Player>();

        for (int i = 0; i < _playerEntries.Count; i++)
        {
            string name = string.IsNullOrWhiteSpace(_playerEntries[i].Text)
                ? $"Player {i + 1}"
                : _playerEntries[i].Text!.Trim();

            players.Add(new Player
            {
                Name = name,
                Score = 0
            });
        }

        await Navigation.PushAsync(
            new MainPage(
                _dbService,
                players,
                _gameId,
                _timerSeconds));
    }
}