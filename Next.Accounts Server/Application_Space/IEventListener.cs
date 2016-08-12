using System;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Application_Space
{
    public interface IEventListener
    {
        void OnException(Exception ex);

        void OnEvent(string message, object sender = null);

        
    }
}