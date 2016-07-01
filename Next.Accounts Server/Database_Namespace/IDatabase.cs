using System.Collections.Generic;
using System.Windows.Documents;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Database_Namespace
{
    public interface IDatabase
    {
        Account GetAccount(Computer holder);

        void ReleaseAccount(Account account);

        Computer GetCopmuter();

        void UpdateComputer(Computer computer);

        int GetCountOfAccounts(bool free = false);

        bool AddAccounts(IList<Account> source);

        bool AddAccount(Account account);

        bool RemoveAccount(Account account);

        bool LogAccountRequire(Account account);

        bool LogAccountRelease(Account account);
    }
}