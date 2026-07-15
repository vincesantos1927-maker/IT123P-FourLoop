using System;
using System.Collections.Generic;
using System.Text;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Services
{
    // Handles the pre-game setup logic: how many players
    public class PlayerSetupService
    {
        public static readonly int[] TimerOptions = { 10, 15, 20, 25, 30 }; // The allowed timer durations (in seconds) that players can cycle through.
        public int PlayerCount { get; private set; } = 2; // Current number of players for this game. Defaults to 2.
        public int TimerSeconds { get; private set; } = 30;  // Current selected timer length in seconds. Defaults to 30.
        public int IncrementPlayerCount()
        {
            PlayerCount = Math.Min(4, PlayerCount + 1);  // Increases player count by 1, capped at 4 max players.
            return PlayerCount;
        }
        public int DecrementPlayerCount() // Decreases player count by 1, 2 min players.
        {
            PlayerCount = Math.Max(2, PlayerCount - 1);
            return PlayerCount;
        }
        public int IncrementTimer() // Moves TimerSeconds one step up in TimerOptions
        {
            int currentIndex = Array.IndexOf(TimerOptions, TimerSeconds);
            TimerSeconds = TimerOptions[Math.Min(TimerOptions.Length - 1, currentIndex + 1)];
            return TimerSeconds;
        }
        public int DecrementTimer() // Moves TimerSeconds one step down in TimerOptions
        {
            int currentIndex = Array.IndexOf(TimerOptions, TimerSeconds);
            TimerSeconds = TimerOptions[Math.Max(0, currentIndex - 1)];
            return TimerSeconds;
        }

        // Builds the actual list of Player objects from the entered names,
        public List<Player> CreatePlayers(IReadOnlyList<string> playerNames)
        {
            if(playerNames.Count < 2 || playerNames.Count > 4)
                throw new ArgumentException("Player count must be between 2 and 4.");
            if (playerNames.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Player names cannot be empty.");
            return playerNames.Select(name => new Player { Name = name.Trim(), Score = 0, IsActive = false }).ToList();
        }
    }
}
