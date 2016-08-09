using System;
using System.Diagnostics;
using System.Windows.Threading;
using Next.Accounts_Client.Web_Space;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Client.Controllers.Realize_Classes
{
    public class ProcessTracker
    {

        public ITrackerListener TrackerListener { get; set; }

        public IListener EventListener { get; set; }

        public IProcessLauncher ProcessLauncher { get; set; }

        private App_data.Settings _settings;

        private bool _hasBeenLaunched = false;

        private DispatcherTimer _timer;

        public ProcessTracker(App_data.Settings settings)
        {
            _settings = settings;
            _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }

        public void StartTimer() =>_timer.Start();

        public void StopTimer() => _timer.Stop();

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            var launched = ProcessLauncher.IsActive(_settings.ProcessName);
            if (launched && !_hasBeenLaunched)
            {
                _hasBeenLaunched = true;
                TrackerListener.OnSteamStarted();
            }
            else if (!launched && _hasBeenLaunched)
            {
                _hasBeenLaunched = false;
                TrackerListener.OnSteamClosed();
            }
        }
    }
}