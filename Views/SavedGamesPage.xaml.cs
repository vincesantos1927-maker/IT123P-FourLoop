using Microsoft.Maui.Controls;

namespace jeo_ano_ba.Views;

public partial class SavedGamesPage : ContentPage
{
    public SavedGamesPage()
    {
        InitializeComponent();
    }

    private async void CloseTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void CreateTapped(object sender, TappedEventArgs e)
    {
        await CreateButton.ScaleToAsync(0.90, 70, Easing.CubicIn);
        await CreateButton.ScaleToAsync(1.00, 120, Easing.SpringOut);

        await Navigation.PushAsync(new NewBoardPage());
    }
}