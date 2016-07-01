using System;
using System.Net;

namespace Next.Accounts_Server.Web_Space
{
    public interface IWebListener
    {
        void OnRequestReceived(HttpListenerRequest request);

        void OnWebError(Exception ex); 
    }
}