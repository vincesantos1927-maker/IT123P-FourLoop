using System;
using System.Collections.Generic;
using System.Text;

namespace jeo_ano_ba.Services
{
    public class GameTimerService
    {
        private CancellationTokenSource? _timerTokenSource;
        public int RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public event Action<int>? Tick;
        public event Action? TimedOut;
        public async Task StartAsync(int seconds)
        {
            Stop();
            RemainingSeconds = Math.Clamp(seconds, 5, 60);
            IsRunning = true;
            _timerTokenSource = new CancellationTokenSource();
            var token = _timerTokenSource.Token;
            try
            {
                while (RemainingSeconds > 0 && !token.IsCancellationRequested)
                {
                    Tick?.Invoke(RemainingSeconds);
                    await Task.Delay(1000, token);
                    RemainingSeconds--;
                }
                if (!token.IsCancellationRequested)
                {
                    IsRunning = false;
                    Tick?.Invoke(0);
                    TimedOut?.Invoke();
                }
            }
            catch (TaskCanceledException)
            {
                // Timer was stopped
            }
        }
        public void Stop()
        {
            _timerTokenSource?.Cancel();
            _timerTokenSource?.Dispose();
            _timerTokenSource = null;
            IsRunning = false;
        }
    }
}
