using System;
using System.Threading;
using System.Windows.Threading;

namespace Next.Accounts_Server.Timers
{
    public class WorkTimer
    {
        private ITimeListener _timeListener;

        private DateTime _startTime;

        private DispatcherTimer _timer;

        public WorkTimer(ITimeListener timeListener)
        {
            _timeListener = timeListener;
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1)};
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            var now = DateTime.Now;
            var difference = now - _startTime;
            _timeListener.UpdateTime(difference);
        }

        public void Stop()
        {
            _timer?.Stop();
        }
    }
}