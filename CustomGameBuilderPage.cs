using System;
using System.Collections.Generic;
using System.Text;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;

namespace jeo_ano_ba;

public class CustomGameBuilderPage : ContentPage
{
    private const int CategoryCount = 6;
    private const int CluesPerCategory = 5;
    private static readonly int[] PointValues = { 100, 200, 300, 400, 500 };

    private readonly GameDatabaseService _dbService;

    private readonly Entry _titleEntry;
    private readonly List<Entry> _categoryNameEntries = new();
    private readonly List<List<Entry>> _questionEntries = new();
    private readonly List<List<Entry>> _answerEntries = new();

    public CustomGameBuilderPage(GameDatabaseService dbService)
    {
        _dbService = dbService;
        
        BackgroundColor = Color.FromArgb("#0D0B1E");

        _titleEntry = new Entry { Placeholder = "Game Title", TextColor = Colors.White, PlaceholderColor = Colors.Gray };

        var root = new VerticalStackLayout { Padding = 20, Spacing = 15 };
        root.Add(new Label { Text = "BUILD YOUR OWN GAME", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FF9800"), HorizontalOptions = LayoutOptions.Center });
        root.Add(_titleEntry);

        for (int c = 0; c < CategoryCount; c++)
        {
            var catNameEntry = new Entry { Placeholder = $"Category {c + 1} name", TextColor = Colors.White, PlaceholderColor = Colors.Gray, FontAttributes = FontAttributes.Bold };
            _categoryNameEntries.Add(catNameEntry);

            var catStack = new VerticalStackLayout { Spacing = 8 };
            catStack.Add(catNameEntry);

            var qList = new List<Entry>();
            var aList = new List<Entry>();

            for (int q = 0; q < CluesPerCategory; q++)
            {
                catStack.Add(new Label { Text = $"${PointValues[q]} clue", TextColor = Colors.Gray, FontSize = 12 });

                var questionEntry = new Entry { Placeholder = "Question (shown to players)", TextColor = Colors.White, PlaceholderColor = Colors.Gray };
                var answerEntry = new Entry { Placeholder = "Correct answer", TextColor = Colors.White, PlaceholderColor = Colors.Gray };

                qList.Add(questionEntry);
                aList.Add(answerEntry);

                catStack.Add(questionEntry);
                catStack.Add(answerEntry);
            }

            _questionEntries.Add(qList);
            _answerEntries.Add(aList);

            root.Add(new Border
            {
                Stroke = Color.FromArgb("#FF9800"),
                StrokeThickness = 1,
                Padding = 10,
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                Content = catStack
            });
        }

        var createButton = new Button
        {
            Text = "CREATE GAME",
            BackgroundColor = Color.FromArgb("#FF9800"),
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold
        };
        createButton.Clicked += OnCreateGameClicked;
        root.Add(createButton);

        Content = new ScrollView { Content = root };
    }

    private async void OnCreateGameClicked(object? sender, EventArgs e)
    {
        string title = string.IsNullOrWhiteSpace(_titleEntry.Text) ? "Custom Game" : _titleEntry.Text.Trim();
        var categories = new List<CustomCategoryInput>();

        for (int c = 0; c < CategoryCount; c++)
        {
            string catName = _categoryNameEntries[c].Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(catName))
            {
                await DisplayAlert("Missing Info", "All 6 category names must be filled.", "OK");
                return;
            }

            var clues = new List<(string, string)>();

            for (int q = 0; q < CluesPerCategory; q++)
            {
                string question = _questionEntries[c][q].Text?.Trim() ?? string.Empty;
                string answer = _answerEntries[c][q].Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
                {
                    await DisplayAlert("Missing Info", "All 30 questions and answers must be filled.", "OK");
                    return;
                }

                clues.Add((question, answer));
            }

            categories.Add(new CustomCategoryInput { CategoryName = catName, Clues = clues });
        }
        int gameId = await _dbService.BuildPlayerAuthoredGameAsync(
            title,
            categories,
            startingPointValue: 100,
            pointIncrement: 100);
        await Navigation.PushAsync(new PlayerSetupPage(_dbService, gameId));
    }
}
