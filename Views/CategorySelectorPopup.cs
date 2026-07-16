using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Views;

public class CategorySelectorPopup : Popup {
    // Store all categories and selected categories
    private readonly List<CategoryDb> _allCategories;
    private readonly List<CategoryDb> _selectedCategories = new();

    // Save the final selected category names
    public List<string>? FinalSelection { get; private set; }

    // UI controls
    private readonly Label _countBadgeLabel;
    private readonly Button _continueButton;
    private readonly CollectionView _collectionView;
    private readonly SearchBar _searchBar;

    // App theme colors
    private static readonly Color NavyUnselected = Color.FromArgb("#1E3A5F");
    private static readonly Color OliveSelected = Color.FromArgb("#2E2E14");
    private static readonly Color GoldAccent = Color.FromArgb("#FFCC00");

    public CategorySelectorPopup(List<CategoryDb> availableCategories) {
        // Save available categories
        _allCategories = availableCategories;

        Color = Colors.Transparent;

        // Create popup header
        var closeButton = new Button {
            Text = "✕",
            BackgroundColor = Color.FromArgb("#2A2A4E"),
            TextColor = Colors.White,
            WidthRequest = 36,
            HeightRequest = 36,
            CornerRadius = 18,
            Padding = 0,
            FontSize = 16
        };
        closeButton.Clicked += (s, e) => Close(null);

        var titleLabel = new Label {
            Text = "Pick Categories",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center
        };

        // Show selected category count
        _countBadgeLabel = new Label {
            Text = "0/6",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = GoldAccent,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var countBadge = new Border {
            BackgroundColor = Color.FromArgb("#1E3A5F"),
            Padding = new Thickness(12, 4),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = _countBadgeLabel
        };

        var headerGrid = new Grid {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        headerGrid.Add(closeButton, 0, 0);
        headerGrid.Add(titleLabel, 1, 0);
        headerGrid.Add(countBadge, 2, 0);

        // Create search bar
        _searchBar = new SearchBar {
            Placeholder = "Search categories...",
            BackgroundColor = Color.FromArgb("#1E3A5F"),
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#AAB4C8")
        };
        _searchBar.TextChanged += OnSearchTextChanged;

        // Display category list
        _collectionView = new CollectionView {
            SelectionMode = SelectionMode.None,
            ItemsSource = _allCategories,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) {
                ItemSpacing = 3
            },
            ItemTemplate = new DataTemplate(() => {

                // Selection check icon
                var checkCircleLabel = new Label {
                    Text = "✓",
                    TextColor = Color.FromArgb("#0F0F2D"),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                var checkCircle = new Border {
                    BackgroundColor = GoldAccent,
                    WidthRequest = 26,
                    HeightRequest = 26,
                    StrokeShape = new RoundRectangle { CornerRadius = 13 },
                    Content = checkCircleLabel,
                    Opacity = 0
                };

                var nameLabel = new Label {
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 15,
                    VerticalOptions = LayoutOptions.Center
                };
                nameLabel.SetBinding(Label.TextProperty, "Name");

                var rowGrid = new Grid {
                    ColumnDefinitions = {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    Padding = new Thickness(16, 14)
                };

                rowGrid.Add(nameLabel, 0, 0);
                rowGrid.Add(checkCircle, 1, 0);

                var pillBorder = new Border {
                    BackgroundColor = NavyUnselected,
                    Stroke = NavyUnselected,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Content = rowGrid,
                    Margin = new Thickness(0)
                };

                // Handle category selection
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => {
                    if (pillBorder.BindingContext is CategoryDb category)
                        ToggleCategory(category, pillBorder, checkCircle);
                };
                pillBorder.GestureRecognizers.Add(tapGesture);

                return pillBorder;
            })
        };

        // Continue button
        _continueButton = new Button {
            Text = "Continue — 0/6 selected",
            BackgroundColor = Color.FromArgb("#5C5A1E"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 16,
            HeightRequest = 52,
            IsEnabled = false
        };
        _continueButton.Clicked += OnGenerateClicked;

        // Build popup layout
        var mainGrid = new Grid {
            Padding = 20,
            RowSpacing = 14,
            WidthRequest = 420,
            HeightRequest = 620,
            BackgroundColor = Color.FromArgb("#0F0F2D"),
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        mainGrid.Add(headerGrid, 0, 0);
        mainGrid.Add(_searchBar, 0, 1);
        mainGrid.Add(_collectionView, 0, 2);
        mainGrid.Add(_continueButton, 0, 3);

        // Display popup content
        Content = new Border {
            BackgroundColor = Color.FromArgb("#0F0F2D"),
            Stroke = GoldAccent,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = mainGrid
        };
    }

    // Select or remove a category
    private void ToggleCategory(CategoryDb category, Border pillBorder, Border checkCircle) {
        bool isCurrentlySelected = _selectedCategories.Contains(category);

        // Limit selection to six categories
        if (!isCurrentlySelected && _selectedCategories.Count >= 6)
            return;

        if (isCurrentlySelected) {
            _selectedCategories.Remove(category);
            pillBorder.BackgroundColor = NavyUnselected;
            pillBorder.Stroke = NavyUnselected;
            checkCircle.Opacity = 0;
        }
        else {
            _selectedCategories.Add(category);
            pillBorder.BackgroundColor = OliveSelected;
            pillBorder.Stroke = GoldAccent;
            checkCircle.Opacity = 1;
        }

        // Update selection count and button state
        _countBadgeLabel.Text = $"{_selectedCategories.Count}/6";
        _continueButton.Text = $"Continue — {_selectedCategories.Count}/6 selected";
        _continueButton.IsEnabled = (_selectedCategories.Count == 6);
        _continueButton.BackgroundColor = (_selectedCategories.Count == 6)
            ? GoldAccent
            : Color.FromArgb("#5C5A1E");
        _continueButton.TextColor = (_selectedCategories.Count == 6)
            ? Color.FromArgb("#0F0F2D")
            : Colors.White;
    }

    // Filter categories using the search text
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) {
        string query = e.NewTextValue?.Trim().ToLower() ?? "";

        _collectionView.ItemsSource = string.IsNullOrEmpty(query)
            ? _allCategories
            : _allCategories.Where(c => c.Name.ToLower().Contains(query)).ToList();
    }

    // Return selected categories
    private void OnGenerateClicked(object? sender, EventArgs e) {
        if (_selectedCategories.Count == 6) {
            FinalSelection = _selectedCategories.Select(c => c.Name).ToList();
            Close(FinalSelection);
        }
    }
}