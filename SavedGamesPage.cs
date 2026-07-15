using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using jeo_ano_ba.Views;

namespace jeo_ano_ba;

public class SavedGamesPage : ContentPage {
    private readonly GameDatabaseService _dbService;
    private readonly CollectionView _gamesList = new();

    public SavedGamesPage(GameDatabaseService dbService) {
        _dbService = dbService;
        Title = "Saved Games";

        var addButton = new Button { Text = "+" };
        addButton.Clicked += async (_, _) => {
            await Navigation.PushAsync(new CustomGameBuilderPage(_dbService));
        };

        _gamesList.ItemTemplate = new DataTemplate(() => {
            var row = new Grid {
                Padding = new Thickness(10),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var openButton = new Button {
                BackgroundColor = Color.FromArgb("#1E1E3F"),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Fill
            };

            // This binds directly to database GameDb Name!
            openButton.SetBinding(Button.TextProperty, nameof(GameDb.Name));

            openButton.Clicked += async (sender, _) => {
                if ((sender as Button)?.BindingContext is GameDb game) {
                    await Navigation.PushAsync(new PlayerSetupPage(_dbService, game.Id));
                }
            };

            var deleteButton = new Button {
                Text = "X",
                BackgroundColor = Colors.DarkRed,
                TextColor = Colors.White,
                WidthRequest = 50,
            };

            deleteButton.Clicked += async (sender, _) => {
                if ((sender as Button)?.BindingContext is not GameDb game)
                    return;

                bool confirm = await DisplayAlert("Delete Game", $"Delete {game.Name}?", "Yes", "No");
                if (!confirm)
                    return;

                await _dbService.DeleteGameAsync(game.Id);
                await LoadGamesAsync();
            };

            row.Add(openButton, 0, 0);
            row.Add(deleteButton, 1, 0);
            return row;
        });

        _gamesList.SelectionMode = SelectionMode.None;

        Content = new VerticalStackLayout {
            Padding = 20,
            Spacing = 10,
            Children =
            {
                new Label { Text = "Saved Games", FontSize = 22 },
                addButton,
                _gamesList
            }
        };
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        await LoadGamesAsync();
    }

    private async Task LoadGamesAsync() {
        var games = await _dbService.GetAllGamesAsync();

        // This passes the GameDb list directly to the list view
        _gamesList.ItemsSource = games
            .Where(game => !game.IsPreset && game.Name != "Master Library")
            .ToList();
    }
}