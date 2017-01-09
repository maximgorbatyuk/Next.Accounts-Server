using System.Threading.Tasks;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space
{
    public interface IRequestSender
    {
        Task<bool> SendPostDataAsync(ApiMessage message, string url = null);

        //Task<bool> SendAccountRequestAsync(bool withoutVacBan = false);

        //Task<bool> SendAccountReleaseAsync(Account account);
    }
}