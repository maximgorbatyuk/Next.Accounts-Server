using System.Net;
using System.Threading.Tasks;

namespace Next.Accounts_Server.Web_Space
{
    public interface IGetResponder
    {
        Task<string> GetHtmlPage(HttpListenerContext context, string raw = "/", string message = null, bool error = false);
    }
}