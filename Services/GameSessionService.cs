using System;
using System.Collections.Generic;
using System.Text;
using jeo_ano_ba.Models;

namespace jeo_ano_ba.Services
{
    public class GameSessionService
    {
        private readonly HashSet<int> _lockedClueIds = new();
        public GameDb? CurrentGame { get; private set; }
        public List<Player> Players { get; private set; } = new();
        public ClueDb? CurrentClue { get; private set; }
        public Player? BuzzedPlayer { get; private set; }
        public Player? CurrentPicker { get; private set; }
        public int TimerSeconds { get; set; } = 30;
        public bool IsAnswerRevealed { get; private set; }
        //sets the game to 0
        public void StartGame(GameDb game, List<Player> players)
        {
            CurrentGame = game;
            Players = players;
            TimerSeconds = Math.Clamp(TimerSeconds, 10, 30);
            CurrentClue = null;
            BuzzedPlayer = null;
            CurrentPicker = players.FirstOrDefault();
            IsAnswerRevealed = false;
            _lockedClueIds.Clear();

            foreach (var player in Players)
            {
                player.Score = 0;
                player.IsActive = false;
            }
            foreach (var clue in game.Categories.SelectMany(cat => cat.Clues))
            {
                clue.IsCompleted = false;
            }
        }
        //when choosing a question
        public bool SelectClue(ClueDb clue)
        {
            if (IsClueLocked(clue)) return false;
            CurrentClue = clue;
            IsAnswerRevealed = false;
            BuzzedPlayer = null;
            foreach (var player in Players)
            {
                player.IsActive = false;

            }
            return true;
        }
        //first player to buzz gets to answer and pick a question
        public bool Buzz(Player player)
        {
            if (CurrentClue == null || BuzzedPlayer != null) return false;
            BuzzedPlayer = player;
            CurrentPicker = player;
            foreach (var p in Players)
            {
                p.IsActive = p == player;
            }
            return true;
        }
        public bool RevealAnswer()
        {
            if (CurrentClue == null) return false;
            IsAnswerRevealed = true;
            return true;
        }
        public ClueResult? MarkCorrect()
        {
            if (CurrentClue == null || BuzzedPlayer == null) return null;
            BuzzedPlayer.Score += CurrentClue.PointValue;
            return LockAndFinishClue(wasCorrect: true);
        }
        public ClueResult? MarkIncorrect()
        {
            if (CurrentClue == null || BuzzedPlayer == null) return null;
            BuzzedPlayer.Score -= CurrentClue.PointValue;
            return LockAndFinishClue(wasCorrect: false);
        }
        public ClueResult? MarkTimeout()
        {
            IsAnswerRevealed = true;
            return MarkIncorrect();
        }
        public void AdjustScore(Player player, int amount)
        {
            player.Score += amount;
        }
        public bool IsClueLocked(ClueDb clue)
        {
            return clue.Id > 0 ? _lockedClueIds.Contains(clue.Id) : clue.IsCompleted;
        }
        public bool IsGameOver()
        {
            if (CurrentGame == null) return false;
            return CurrentGame.Categories.SelectMany(category => category.Clues).All(IsClueLocked);
        }
        public List<Player> GetLeaderboard()
        {
            return Players.OrderByDescending(player => player.Score).ToList();
        }
        public Player? GetWinner()
        {
            var leaderboard = GetLeaderboard();
            if (leaderboard.Count == 0) return null;
            int topScore = leaderboard[0].Score;
            int tiedPlayers = leaderboard.Count(player => player.Score >= topScore);
            return tiedPlayers == 1 ? leaderboard[0] : null;
        }
        public void ResetCurrentClue()
        {
            CurrentClue = null;
            BuzzedPlayer = null;
            IsAnswerRevealed = false;
            foreach (var player in Players)
            {
                player.IsActive = false;
            }
        }
        public void ResetGame()
        {
            CurrentGame = null;
            Players.Clear();
            CurrentClue = null;
            BuzzedPlayer = null;
            CurrentPicker = null;
            IsAnswerRevealed = false;
            _lockedClueIds.Clear();
            foreach (var player in Players)
            {
                player.Score = 0;
                player.IsActive = false;
            }

        }
        private ClueResult LockAndFinishClue(bool wasCorrect)
        {
            var finishedClue = CurrentClue!;
            var answeringPlayer = BuzzedPlayer!;
            finishedClue.IsCompleted = true;
            if (finishedClue.Id > 0)
                _lockedClueIds.Add(finishedClue.Id);
            var result = new ClueResult
            {
                Clue = finishedClue,
                Player = answeringPlayer,
                WasCorrect = wasCorrect,
                UpdatedScore = answeringPlayer.Score,
                GameFinished = IsGameOver()
            };
            ResetCurrentClue();
            return result;
        }
    }
    public class ClueResult
    {
        public required ClueDb Clue { get; init; }
        public required Player Player { get; init; }
        public bool WasCorrect { get; init; }
        public int UpdatedScore { get; init; }
        public bool GameFinished { get; init; }
    }
}
