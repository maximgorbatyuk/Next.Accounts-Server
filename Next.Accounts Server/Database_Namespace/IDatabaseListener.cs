namespace Next.Accounts_Server.Database_Namespace
{
    public interface IDatabaseListener
    {
        void UpdateAccountCount(int count, int available);
    }
}