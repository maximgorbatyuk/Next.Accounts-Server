﻿using System.Collections.Generic;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Controllers
{
    public interface IUsedTracker
    {
        void Start();

        void Stop();

        bool AddAccount(Account account);

        bool AddAccount(IList<Account> accounts);

        bool RemoveAccount(Account account);

        bool ResetTimer(Account account);

        bool Clear();

        List<Account> ClearUpUsed();
    }
}