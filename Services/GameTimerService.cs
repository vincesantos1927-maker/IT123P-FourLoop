using System;
using System.Collections.Generic;
using System.Text;

namespace jeo_ano_ba.Services
{
    //handles countdown, also updates the ui when time runs out
    public class GameTimerService
    {
        //for stoping the timer's loop
        private CancellationTokenSource? _timerTokenSource;
        //checks how many seconds are left
        public int RemainingSeconds { get; private set; }
        //is true if timer is running
        public bool IsRunning { get; private set; }
        //every tick
        public event Action<int>? Tick;
        //when countdown gets to zero
        public event Action? TimedOut;

        //starts timer for a specific range
        public async Task StartAsync(int seconds)
        {
            //stops timers before starting new one
            Stop();
            //restricts timer to only 5 to 60
            RemainingSeconds = Math.Clamp(seconds, 5, 60);
            IsRunning = true;

            //stops if the player answers early
            _timerTokenSource = new CancellationTokenSource();
            var token = _timerTokenSource.Token;
            try
            {
                //keeps running unless timer hasnt gotten to 0 or manually stopped
                while (RemainingSeconds > 0 && !token.IsCancellationRequested)
                {
                    //tells the ui what remaining time it is
                    Tick?.Invoke(RemainingSeconds);
                    await Task.Delay(1000, token);
                    RemainingSeconds--;
                }

                //if loop/the timer runs out
                if (!token.IsCancellationRequested)
                {
                    IsRunning = false;
                    Tick?.Invoke(0); //tells ui timer ran out
                    TimedOut?.Invoke(); //timed out logic
                }
            }
            catch (TaskCanceledException)
            {
                // Timer was stopped
            }
        }
        //stops the timer
        public void Stop()
        {
            _timerTokenSource?.Cancel();
            _timerTokenSource?.Dispose();
            _timerTokenSource = null;
            IsRunning = false;
        }
    }
}
