using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Database_Namespace
{
    public interface IDatabase
    {

        Task<Account> GetAccount(Sender sender);

        Task<int> ReleaseAccount(Account account);

        //void UpdateComputer(Sender Sender);

        Task<int> UpdateAccountAsync(Account account);

        Task<int> UpdateAccountAsync(IList<Account> accounts);

        Task<List<Account>> GetAccounts(bool availableOnly = false);

        Task<List<Account>> GetUsedAccounts();

        Task<int> AddAccountAsync(IList<Account> source);

        Task<int> AddAccountAsync(Account account);

        Task<int> RemoveAccountAsync(Account account);

        Task<int> RestoreAccounts(IList<Account> source);

        Task<int> DeleteAccountsTable();
    }
}