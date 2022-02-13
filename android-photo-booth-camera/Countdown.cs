using System;
using System.Diagnostics;
using System.Timers;

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera
{
    internal sealed class Countdown
    {
        private int _secondsRemaining;
        private Stopwatch _stopwatch;
        private Timer _timer;

        public Countdown(int seconds)
        {
            Seconds = seconds;
        }

        public int Seconds { get; }

        public TimeSpan TimeRemaining => TimeSpan.FromSeconds(Seconds) - _stopwatch.Elapsed;

        public event EventHandler OnCountdownComplete;
        public event EventHandler<int> OnCountdownTick;

        public void Start()
        {
            _secondsRemaining = Seconds;

            _timer = new Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += OnTimerElapsed;

            _stopwatch = Stopwatch.StartNew();

            _timer.Start();
            OnCountdownTick?.Invoke(this, _secondsRemaining); //First tick immediately
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _secondsRemaining--;

            OnCountdownTick?.Invoke(this, _secondsRemaining);

            if (_secondsRemaining == 0)
            {
                _timer.Stop();

                OnCountdownComplete?.Invoke(this, EventArgs.Empty);

                _timer.Dispose();
                _timer = null;
            }
        }
    }
}