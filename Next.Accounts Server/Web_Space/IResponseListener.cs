using System;

namespace Next.Accounts_Server.Web_Space
{
    public interface IResponseListener
    {
        void OnServerResponse(string responseString);

        void OnConnectionError(Exception ex);
    }
}