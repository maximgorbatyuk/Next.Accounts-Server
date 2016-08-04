using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Database_Namespace
{
    public interface IDatabase
    {
        Task<Account> GetAccount(Computer computer, bool free = false);

        Task<int> ReleaseAccount(Account account);

        //void UpdateComputer(Computer computer);

        Task<int> UpdateAccountAsync(Account account);

        Task<List<Account>> GetListOfAccountsAsync(bool free = false);

        Task<int> AddAccountsAsync(IList<Account> source);

        Task<int> AddAccountAsync(Account account);

        Task<int> RemoveAccountAsync(Account account);

    }
}