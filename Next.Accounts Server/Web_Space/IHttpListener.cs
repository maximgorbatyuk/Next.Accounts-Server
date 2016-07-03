using System;
using System.Net;

namespace Next.Accounts_Server.Web_Space
{
    public interface IHttpListener
    {
        void OnRequestReceived(HttpListenerContext context);
    }
}