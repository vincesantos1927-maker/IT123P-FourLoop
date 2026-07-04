using jeo_ano_ba.Services;

namespace jeo_ano_ba
{
    public partial class App : Application
    {
        private readonly StartPage _startPage;

        public App(StartPage startPage)
        {
            InitializeComponent();
            _startPage = startPage;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                return new Window(new NavigationPage(_startPage));
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