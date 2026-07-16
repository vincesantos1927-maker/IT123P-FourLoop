using jeo_ano_ba.Models;
using jeo_ano_ba.ViewModels;

namespace jeo_ano_ba.Services
{
    //manages the game's configuration/settings before starting
    public class PlayerSetupService
    {
        //limits players and timers
        private const int MinPlayers = 2;
        private const int MaxPlayers = 4;
        private const int MinTimerSeconds = 5;
        private const int MaxTimerSeconds = 60;
        private const int TimerStepSeconds = 5; //amount of seconds changed
        //fixed config state, default
        public int PlayerCount { get; private set; } = 2;
        public int TimerSeconds { get; private set; } = 30;
        //add players
        public int IncrementPlayerCount()
        {
            PlayerCount = Math.Min(MaxPlayers, PlayerCount + 1);
            return PlayerCount;
        }
        //lessens players
        public int DecrementPlayerCount()
        {
            PlayerCount = Math.Max(MinPlayers, PlayerCount - 1);
            return PlayerCount;
        }
        //increment by 5s
        public int IncrementTimer()
        {
            TimerSeconds = Math.Min(MaxTimerSeconds, TimerSeconds + TimerStepSeconds);
            return TimerSeconds;
        }
        //decreases by 5
        public int DecrementTimer()
        {
            TimerSeconds = Math.Max(MinTimerSeconds, TimerSeconds - TimerStepSeconds);
            return TimerSeconds;
        }
        //takes names and converts them into structured "player models
        // list of names entered in the setup UI
        public List<Player> CreatePlayers(IReadOnlyList<string> playerNames)
        {
            //prevents start if there's not enough or theres too much players
            if (playerNames.Count < MinPlayers || playerNames.Count > MaxPlayers)
                throw new ArgumentException("Player count must be between 2 and 4.");
            //converts strings into player objects
            return playerNames
                .Select((name, index) => new Player
                {
                    //if blank name, automatically names them
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