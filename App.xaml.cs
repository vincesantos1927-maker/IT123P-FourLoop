using jeo_ano_ba.Services;
using jeo_ano_ba.Views;

namespace jeo_ano_ba
{
    public partial class App : Application
    {
        private readonly MainMenuPage _mainMenuPage;

        public App(MainMenuPage mainMenuPage)
        {
            InitializeComponent();
            _mainMenuPage = mainMenuPage;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                return new Window(new NavigationPage(_mainMenuPage));
            }
            catch (Exception ex)
            {
                // This forces Windows to show you the real inner error message before crashing
                MainPage = new ContentPage
                {
                    Content = new ScrollView
                    {
                        Content = new Label { Text = $"CRASH ERROR: {ex.ToString()}", TextColor = Colors.Red, Padding = 20 }
                    }
                };
                return new Window(MainPage);
            }
        }
    }
}