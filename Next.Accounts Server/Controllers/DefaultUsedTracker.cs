using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Controllers
{
    public class DefaultUsedTracker : IUsedTracker
    {
        private readonly Dictionary<Account, int> _usedAccounts;

        public int MinuteLimit { get; set; }

        private readonly DispatcherTimer _timer;

        public DefaultUsedTracker(int minutes = 5)
        {
            MinuteLimit = minutes;
            _usedAccounts = new Dictionary<Account, int>();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(MinuteLimit) };
            _timer.Tick += UsedTrackerTimerTick;
            
        }

        private void UsedTrackerTimerTick(object sender, EventArgs eventArgs)
        {
            if (_usedAccounts == null) return;

        }


        public void Start()
        {
            _timer?.Start();
        }

        public void Stop()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
        }

        public bool AddAccount(Account account)
        {
            var lastCount = _usedAccounts.Count;
            _usedAccounts.Add(account, 0);
            return _usedAccounts.Count > lastCount;
        } 
            

        public bool AddAccount(IList<Account> accounts)
        {
            var last = _usedAccounts.Count;
            foreach (var a in accounts)
            {
                AddAccount(a);
            }
            return _usedAccounts.Count > last;
        }

        public bool RemoveAccount(Account account)
        {
            var last = _usedAccounts.Count;
            var removeTo = _usedAccounts.Keys.Where(a => a.Id == account.Id).ToList();
            if (!removeTo.Any()) return false;
            foreach (var acc in removeTo)
            {
                _usedAccounts.Remove(acc);
            }
            return _usedAccounts.Count > last;
        }
            

        public bool ResetTimer(Account account)
        {
            var result = false;
            foreach (var key in _usedAccounts.Keys)
            {
                if (key.Id != account.Id) continue;
                _usedAccounts[key] = 0;
                result = true;
            }
            return result;
        }

        public bool Clear()
        {
            _usedAccounts.Clear();
            return true;
        }

        public List<Account> ClearUpUsed()
        {
            var result = (from pair in _usedAccounts where pair.Value > MinuteLimit select pair.Key).ToList();
            return result;
        }
    }
}