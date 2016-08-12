using System;
using System.Diagnostics;
using System.Windows.Threading;
using Next.Accounts_Client.Application_Space;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Client.Controllers.Realize_Classes
{
    public class ProcessTracker
    {

        public ITrackerListener TrackerListener { get; set; }

        public IEventListener EventListener { get; set; }

        public IProcessLauncher ProcessLauncher { get; set; }

        private ClientSettings _clientSettings;

        private bool _hasBeenLaunched = false;

        private DispatcherTimer _timer;

        public ProcessTracker(ClientSettings clientSettings)
        {
            _clientSettings = clientSettings;
            _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }

        public void StartTimer() =>_timer.Start();

        public void StopTimer() => _timer.Stop();

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            var launched = ProcessLauncher.IsActive(_clientSettings.ProcessName);
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