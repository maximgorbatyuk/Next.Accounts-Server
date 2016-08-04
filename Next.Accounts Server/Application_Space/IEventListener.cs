using System;

namespace Next.Accounts_Server.Application_Space
{
    public interface IEventListener
    {
        void OnException(Exception ex);

        void OnMessage(string message);

        void UpdateAccountCount(int count, int available);
    }
}