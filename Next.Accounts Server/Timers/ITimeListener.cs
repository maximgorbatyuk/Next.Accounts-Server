using System;

namespace Next.Accounts_Server.Timers
{
    public interface ITimeListener
    {
        void UpdateTime(TimeSpan difference);
    }
}