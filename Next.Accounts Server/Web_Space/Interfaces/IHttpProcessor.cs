using System.Net;

namespace Next.Accounts_Server.Web_Space.Interfaces
{
    public interface IHttpProcessor
    {
        void OnRequestReceived(HttpListenerContext context);

        void ReturnWebError(HttpListenerContext context, string message, int code = 404);
    }
}