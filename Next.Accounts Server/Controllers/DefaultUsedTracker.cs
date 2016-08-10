using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Controllers
{
    public class DefaultUsedTracker : IUsedTracker, IDisposable
    {
        private Dictionary<Account, int> _usedAccounts;

        private readonly int _minute;

        public DefaultUsedTracker(int minutes = 5)
        {
            _usedAccounts = new Dictionary<Account, int>();
            _minute = minutes;
        }

        public int GetUsedCount()
        {
            return _usedAccounts.Count;
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
            foreach (var acc in removeTo.ToList())
            {
                _usedAccounts.Remove(acc);
            }
            return _usedAccounts.Count < last;
        }
            

        public bool ResetTimer(Account account)
        {
            var result = false;
            foreach (var key in _usedAccounts.Keys.ToList())
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
            List<Account> result = null;
            foreach (var key in _usedAccounts.Keys.ToList())
            {
                if (_usedAccounts[key] <= _minute) continue;

                if (result == null) result = new List<Account>();

                result.Add(key);
                RemoveAccount(key);
            }
            //result = (from pair in _usedAccounts where pair.Value > _minute select pair.Key).ToList();
            return result;
        }

        public void IncreaseTime()
        {
            foreach (var key in _usedAccounts.Keys.ToList())
            {
                _usedAccounts[key] += 1;
            }
        }

        public void Dispose()
        {
            _usedAccounts = null;
        }
    }
}