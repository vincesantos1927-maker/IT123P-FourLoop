using jeo_ano_ba.Models;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Services
{
    public class PlayerSetupService
    {
        private const int MinPlayers = 2;
        private const int MaxPlayers = 4;
        private const int MinTimerSeconds = 5;
        private const int MaxTimerSeconds = 60;
        private const int TimerStepSeconds = 5;

        public int PlayerCount { get; private set; } = 2;
        public int TimerSeconds { get; private set; } = 30;

        public int IncrementPlayerCount()
        {
            PlayerCount = Math.Min(MaxPlayers, PlayerCount + 1);
            return PlayerCount;
        }

        public int DecrementPlayerCount()
        {
            PlayerCount = Math.Max(MinPlayers, PlayerCount - 1);
            return PlayerCount;
        }

        public int IncrementTimer()
        {
            TimerSeconds = Math.Min(MaxTimerSeconds, TimerSeconds + TimerStepSeconds);
            return TimerSeconds;
        }

        public int DecrementTimer()
        {
            TimerSeconds = Math.Max(MinTimerSeconds, TimerSeconds - TimerStepSeconds);
            return TimerSeconds;
        }

        public List<Player> CreatePlayers(IReadOnlyList<string> playerNames)
        {
            if (playerNames.Count < MinPlayers || playerNames.Count > MaxPlayers)
                throw new ArgumentException("Player count must be between 2 and 4.");

            return playerNames
                .Select((name, index) => new Player
                {
                    Name = string.IsNullOrWhiteSpace(name)
                        ? $"Player {index + 1}"
                        : name.Trim(),
                    Score = 0,
                    IsActive = false
                })
                .ToList();
        }
    }
}