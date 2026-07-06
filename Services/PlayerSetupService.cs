using System;
using System.Collections.Generic;
using System.Text;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Services
{
    public class PlayerSetupService
    {
        public static readonly int[] TimerOptions = { 10, 15, 20, 25, 30 };
        public int PlayerCount { get; private set; } = 2;
        public int TimerSeconds { get; private set; } = 30;
        public int IncrementPlayerCount()
        {
            PlayerCount = Math.Min(4, PlayerCount + 1);
            return PlayerCount;
        }
        public int DecrementPlayerCount()
        {
            PlayerCount = Math.Max(2, PlayerCount - 1);
            return PlayerCount;
        }
        public int IncrementTimer()
        {
            int currentIndex = Array.IndexOf(TimerOptions, TimerSeconds);
            TimerSeconds = TimerOptions[Math.Min(TimerOptions.Length - 1, currentIndex + 1)];
            return TimerSeconds;
        }
        public int DecrementTimer()
        {
            int currentIndex = Array.IndexOf(TimerOptions, TimerSeconds);
            TimerSeconds = TimerOptions[Math.Max(0, currentIndex - 1)];
            return TimerSeconds;
        }
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
