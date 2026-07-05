using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace jeo_ano_ba.Views;

public partial class NewBoardPage : ContentPage
{
    private readonly int[] values = { 100, 200, 300, 400, 500 };
    private Border? _selectedTile;
    public NewBoardPage()
    {
        InitializeComponent();

        BuildBoard();
    }

    private void BuildBoard()
    {
        BoardGrid.ColumnSpacing = 8;
        BoardGrid.RowSpacing = 8;

        // 6 columns
        for (int i = 0; i < 6; i++)
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 150 });

        // Header
        BoardGrid.RowDefinitions.Add(new RowDefinition { Height = 50 });

        // 5 question rows
        for (int i = 0; i < 5; i++)
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = 150 });

        for (int col = 0; col < 6; col++)
        {
            var entry = new Entry
            {
                Placeholder = "Category",
                PlaceholderColor = Color.FromArgb("#6D7B93"),
                TextColor = Color.FromArgb("#FFD700"),
                HorizontalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Colors.Transparent
            };

            var header = new Border
            {
                BackgroundColor = Color.FromArgb("#102646"),
                Stroke = Color.FromArgb("#D7B53C"),
                StrokeThickness = 0.5,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(14)
                },
                Content = entry
            };

            Grid.SetRow(header, 0);
            Grid.SetColumn(header, col);

            BoardGrid.Children.Add(header);
        }

        for (int row = 1; row <= 5; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                int value = values[row - 1];

                var label = new Label
                {
                    Text = value.ToString(),
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#7A7233"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                var tile = new Border
                {
                    BackgroundColor = Color.FromArgb("#284C84"),
                    Stroke = Color.FromArgb("#375D99"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = new CornerRadius(16)
                    },
                    Content = label
                };

                var tap = new TapGestureRecognizer();

                tap.Tapped += (s, e) =>
                {
                    // Remove highlight from previously selected tile
                    if (_selectedTile != null)
                    {
                        _selectedTile.StrokeThickness = 1;
                        _selectedTile.Stroke = Color.FromArgb("#375D99");
                    }

                    // Highlight current tile
                    _selectedTile = tile;
                    _selectedTile.StrokeThickness = 2;
                    _selectedTile.Stroke = Color.FromArgb("#FFD700");

                    PopupValueLabel.Text = $"${value}";

                    QuestionEditor.Text = "";
                    AnswerEntry.Text = "";

                    PopupOverlay.IsVisible = true;
                };

                tile.GestureRecognizers.Add(tap);

                Grid.SetRow(tile, row);
                Grid.SetColumn(tile, col);

                BoardGrid.Children.Add(tile);
            }
        }
    }

    private void ClosePopupTapped(object sender, TappedEventArgs e)
    {
        PopupOverlay.IsVisible = false;

        if (_selectedTile != null)
        {
            _selectedTile.StrokeThickness = 1;
            _selectedTile.Stroke = Color.FromArgb("#375D99");

            _selectedTile = null;
        }
    }

    private async void SaveQuestionTapped(object sender, TappedEventArgs e)
    {
        // Temporary

        PopupOverlay.IsVisible = false;

        await DisplayAlertAsync(
            "Saved",
            "Question saved successfully.",
            "OK");

        if (_selectedTile != null)
        {
            _selectedTile.StrokeThickness = 1;
            _selectedTile.Stroke = Color.FromArgb("#375D99");

            _selectedTile = null;
        }
    }

    private async void CloseTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void SaveOnlyTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlertAsync("Save", "Save Only clicked.", "OK");
    }

    private async void SaveStartTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlertAsync("Start", "Save & Start clicked.", "OK");
    }
}