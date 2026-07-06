using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.Models; // Ensure this matches your project structure

namespace jeo_ano_ba.Services;

public class CategorySelectorPopup : Popup
{
    // Use a list of objects to track selection
    private readonly List<CategoryDb> _selectedCategories = new();
    //stores the text of the selected categories to be used in the game
    public List<string>? FinalSelection { get; private set; }

    // ung label na "Selected: " sa taas
    private readonly Label _countTrackerLabel;
    //button that the user clicks to finish creating the game board
    private readonly Button _generateButton;

    // pang popup UI layout, the available categories, action buttons
    public CategorySelectorPopup(List<CategoryDb> availableCategories)
    {
        _countTrackerLabel = new Label
        {
            Text = "Selected: 0 / 6",
            FontSize = 14,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        _generateButton = new Button
        {
            Text = "GENERATE",
            BackgroundColor = Color.FromArgb("#FF9800"),
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            IsEnabled = false
        };
        _generateButton.Clicked += OnGenerateClicked;

        var cancelButton = new Button
        {
            Text = "CANCEL",
            BackgroundColor = Color.FromArgb("#333333"),
            TextColor = Colors.White
        };
        cancelButton.Clicked += (s, e) => Close(null);

        var collectionView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsSource = availableCategories, // Now passing List<CategoryDb>
            ItemTemplate = new DataTemplate(() =>
            {
                var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };

                var checkBox = new CheckBox();
                // Bind to the object's properties
                checkBox.CheckedChanged += OnCategoryCheckedChanged;

                var label = new Label
                {
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.White
                };
                label.SetBinding(Label.TextProperty, "Name"); // Binding to CategoryDb.Name

                grid.Add(checkBox, 0, 0);
                grid.Add(label, 1, 0);

                return grid;
            })
        };

        // 🚀 FIXED: Grid layout with a Star row for the CollectionView so the
        // action buttons (Cancel/Generate) always stay visible instead of being
        // pushed off-screen by an unbounded VerticalStackLayout.
        var mainGrid = new Grid
        {
            Padding = 20,
            RowSpacing = 10,
            WidthRequest = 350,
            HeightRequest = 500,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), // title
                new RowDefinition(GridLength.Auto), // count tracker
                new RowDefinition(GridLength.Star),  // collectionview — takes remaining space
                new RowDefinition(GridLength.Auto)   // buttons
            }
        };

        var titleLabel = new Label
        {
            Text = "SELECT 6 CATEGORIES",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FF9800"),
            HorizontalOptions = LayoutOptions.Center
        };

        mainGrid.Add(titleLabel, 0, 0);
        mainGrid.Add(_countTrackerLabel, 0, 1);
        mainGrid.Add(collectionView, 0, 2);

        var actionGrid = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
        actionGrid.Add(cancelButton, 0);
        actionGrid.Add(_generateButton, 1);
        mainGrid.Add(actionGrid, 0, 3);

        Content = new Border
        {
            BackgroundColor = Color.FromArgb("#1E1E1E"),
            Stroke = Color.FromArgb("#FF9800"),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = mainGrid
        };
    }
    // categories checkbox validation, if the user selects more than 6 categories, the checkbox will be unchecked and the user will be notified
    private void OnCategoryCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is CategoryDb category)
        {
            // Update the model property
            category.IsSelected = e.Value;

            // Maintain your selection list
            if (_selectedCategories.Count >= 6 && !_selectedCategories.Contains(category))
            {
                checkBox.IsChecked = false;
                return;
            }

            if (!_selectedCategories.Contains(category))
                _selectedCategories.Add(category);
            else
            {
                _selectedCategories.Remove(category);
            }

            // Update your UI feedback
            _countTrackerLabel.Text = $"Selected: {_selectedCategories.Count} / 6";
            _generateButton.IsEnabled = (_selectedCategories.Count == 6);
        }
    }
    // When the user clicks the "GENERATE" button, this method will be called to finalize the selection and close the popup.
    private void OnGenerateClicked(object? sender, EventArgs e)
    {
        if (_selectedCategories.Count == 6)
        {
            // Extract the names for your final game generation
            FinalSelection = _selectedCategories.Select(c => c.Name).ToList();
            Close(FinalSelection);
        }
    }
}