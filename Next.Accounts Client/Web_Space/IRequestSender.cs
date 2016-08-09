using System.Threading.Tasks;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Client.Web_Space
{
    public interface IRequestSender
    {
        Task<bool> SendPostDataAsync(ApiMessage message);
    }
}