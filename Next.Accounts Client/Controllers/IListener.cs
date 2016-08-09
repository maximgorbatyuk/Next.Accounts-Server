using System;

namespace Next.Accounts_Client.Controllers
{
    public interface IListener
    {
        void OnEvent(string message, object sender = null);

        void OnException(Exception ex);
    }
}