using System;
using System.Windows.Threading;

namespace WPFZoomPanel.Helpers
{
    public class KeepAliveTimer
    {
        #region Private Fields

        private readonly DispatcherTimer _timer;
        private TimeSpan? _runTime;
        private DateTime _startTime;

        #endregion Private Fields

        #region Public Constructors + Destructors

        public KeepAliveTimer(TimeSpan time, Action action)
        {
            Time = time;
            Action = action;
            _timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = time };
            _timer.Tick += TimerExpired;
        }

        #endregion Public Constructors + Destructors

        #region Public Properties

        public Action Action { get; set; }
        public bool Running { get; private set; }
        public TimeSpan Time { get; set; }

        #endregion Public Properties

        #region Public Methods

        public TimeSpan GetTimeSpan() => _runTime ?? DateTime.UtcNow.Subtract(_startTime);

        public void Nudge()
        {
            lock (_timer)
            {
                if (!Running)
                {
                    _startTime = DateTime.UtcNow;
                    _runTime = null;
                    _timer.Start();
                    Running = true;
                }
                else
                {
                    //Reset the timer
                    _timer.Stop();
                    _timer.Start();
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void TimerExpired(object? sender, EventArgs e)
        {
            lock (_timer)
            {
                Running = false;
                _timer.Stop();
                _runTime = DateTime.UtcNow.Subtract(_startTime);
                Action();
            }
        }

        #endregion Private Methods
    }
}
