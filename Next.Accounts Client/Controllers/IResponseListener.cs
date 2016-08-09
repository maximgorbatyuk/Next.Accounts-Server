using System;

namespace Next.Accounts_Client.Controllers
{
    public interface IResponseListener
    {
        void OnServerResponse(string responseString);

        void OnConnectionError(Exception ex);
    }
}