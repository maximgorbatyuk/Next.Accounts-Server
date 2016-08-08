using System.Collections.Generic;
using System.Threading.Tasks;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Database_Namespace
{
    public interface IRemoteStorage
    {
        Task<List<Account>> GetAccounts(string predicate = "");

        Task<bool> CheckConnection();
    }
}