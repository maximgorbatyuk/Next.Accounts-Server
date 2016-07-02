using System;

namespace Next.Accounts_Server.Database_Namespace
{
    public interface IDatabaseListener
    {
        void OnDatabaseException(Exception ex);

        void UpdateAccountCount(int count);
    }
}