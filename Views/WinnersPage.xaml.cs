using Microsoft.Extensions.DependencyInjection;
using jeo_ano_ba.Models;
using jeo_ano_ba.Services;
using jeo_ano_ba.ViewModels;
using System.Linq;

namespace jeo_ano_ba.Views;

public partial class WinnersPage : ContentPage
{
    private readonly WinnersViewModel _viewModel;

    public WinnersPage(WinnersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        LoadLeaderboard();
    }

    private async void LoadLeaderboard()
    {
        try
        {
            var leaderboard = await _viewModel.LoadLeaderboardAsync();

            // =========================
            // FIRST PLACE
            // =========================
            if (leaderboard.Count > 0)
            {
                FirstName.Text = leaderboard[0].PlayerName;
                FirstScore.Text = leaderboard[0].Score.ToString();
            }

            // =========================
            // SECOND PLACE
            // =========================
            if (leaderboard.Count > 1)
            {
                SecondName.Text = leaderboard[1].PlayerName;
                SecondScore.Text = leaderboard[1].Score.ToString();
            }

            // =========================
            // THIRD PLACE
            // =========================
            if (leaderboard.Count > 2)
            {
                ThirdName.Text = leaderboard[2].PlayerName;
                ThirdScore.Text = leaderboard[2].Score.ToString();
            }

            // Remaining Players (4th onwards)
            LeaderboardCollection.ItemsSource =
                leaderboard.Skip(3).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void HomeTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}