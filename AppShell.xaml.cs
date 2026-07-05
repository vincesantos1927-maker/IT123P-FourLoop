using jeo_ano_ba.Views;

namespace jeo_ano_ba;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SavedGamesPage), typeof(SavedGamesPage));

        Routing.RegisterRoute(nameof(NewBoardPage), typeof(NewBoardPage));
    }
}