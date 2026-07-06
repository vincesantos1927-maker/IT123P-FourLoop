using jeo_ano_ba.Views;

namespace jeo_ano_ba;

public partial class App : Application
{
    private readonly MainMenuPage _mainMenuPage;

    public App(MainMenuPage mainMenuPage)
    {
        InitializeComponent();

        _mainMenuPage = mainMenuPage;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        return new Window(
            new NavigationPage(_mainMenuPage));
    }
}