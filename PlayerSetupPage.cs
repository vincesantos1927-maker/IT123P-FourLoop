using System;
using System.Collections.Generic;
using System.Text;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba;

public class PlayerSetupPage : ContentPage
{
    private readonly GameDatabaseService _dbService;
    private readonly int _gameId;
    private readonly Picker _playerCountPicker = new();
    private readonly Picker _timerPicker = new();
    private readonly VerticalStackLayout _namesStack = new();
    private readonly List<Entry> _nameEntries = new();

    public PlayerSetupPage(GameDatabaseService dbService, int gameId)
    {
        _dbService = dbService;
        _gameId = gameId;

        _playerCountPicker.ItemsSource = new List<int> { 2, 3, 4 };
        _playerCountPicker.SelectedIndex = 0;
        _playerCountPicker.SelectedIndexChanged += (_, _) => BuildNameInputs();

        _timerPicker.ItemsSource = new List<int> { 10, 15, 20, 25, 30 };
        _timerPicker.SelectedIndex = 4;

        var startButton = new Button { Text = "Start Game" };
        startButton.Clicked += OnStartGameClicked;

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Children =
            {
                new Label { Text = "Player Setup", FontSize = 28 },
                _playerCountPicker,
                _namesStack,
                new Label { Text = "Timer Duration" },
                _timerPicker,
                startButton
            }
        };

        BuildNameInputs();
    }

    private void BuildNameInputs()
    {
        _namesStack.Children.Clear();
        _nameEntries.Clear();

        int count = (int)_playerCountPicker.SelectedItem;

        for (int i = 0; i < count; i++)
        {
            var entry = new Entry { Placeholder = $"Player {i + 1} name" };
            _nameEntries.Add(entry);
            _namesStack.Children.Add(entry);
        }
    }

    private async void OnStartGameClicked(object? sender, EventArgs e)
    {
        var players = _nameEntries
            .Select((entry, index) => new Player
            {
                Name = string.IsNullOrWhiteSpace(entry.Text) ? $"Player {index + 1}" : entry.Text.Trim(),
                Score = 0
            })
            .ToList();

        int timerSeconds = (int)_timerPicker.SelectedItem;
        await Navigation.PushAsync(new MainPage(_dbService, players, _gameId, timerSeconds));
    }
}