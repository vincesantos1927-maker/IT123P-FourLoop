using jeo_ano_ba.Views;

namespace jeo_ano_ba.Views;

public partial class MainMenuPage : ContentPage
{
    private bool _musicEnabled = true;

    public MainMenuPage()
    {
        InitializeComponent();
    }

    private async Task AnimateButton(Border button)
    {
        await button.ScaleToAsync(0.96, 80, Easing.CubicIn);
        await button.ScaleToAsync(1.02, 120, Easing.CubicOut);
        await button.ScaleToAsync(1.00, 80, Easing.SpringOut);
    }

    private async void CustomGameTapped(object sender, TappedEventArgs e)
    {
        await AnimateButton(CustomGameButton);

        await Shell.Current.GoToAsync(nameof(SavedGamesPage));
    }

    private async void GeneralKnowledgeTapped(object sender, TappedEventArgs e)
    {
        await AnimateButton(GeneralKnowledgeButton);

        await DisplayAlertAsync(
            "General Knowledge",
            "This feature will connect to the Trivia API later.",
            "OK");
    }

    private async void MusicTapped(object sender, TappedEventArgs e)
    {
        await MusicButton.ScaleToAsync(0.92, 80, Easing.CubicIn);
        await MusicButton.ScaleToAsync(1.0, 120, Easing.SpringOut);

        _musicEnabled = !_musicEnabled;

        if (_musicEnabled)
        {
            MusicIcon.Text = "🔊";
            MusicButton.BackgroundColor = Color.FromArgb("#2F5D95");
        }
        else
        {
            MusicIcon.Text = "🔇";
            MusicButton.BackgroundColor = Color.FromArgb("#234A78");
        }
    }
}