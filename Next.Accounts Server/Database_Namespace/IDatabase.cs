using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Database_Namespace
{
    public interface IDatabase
    {
        Task<Account> GetAccount(Sender sender, bool free = false);

        Task<int> ReleaseAccount(Account account);

        //void UpdateComputer(Sender Sender);

        Task<int> UpdateAccountAsync(Account account);

        Task<int> UpdateAccountAsync(IList<Account> accounts);

        Task<List<Account>> GetListOfAccountsAsync(bool free = false);

        Task<int> AddAccountAsync(IList<Account> source);

        Task<int> AddAccountAsync(Account account);

        Task<int> RemoveAccountAsync(Account account);

        Task<int> RestoreAccounts(IList<Account> source);

    }
}