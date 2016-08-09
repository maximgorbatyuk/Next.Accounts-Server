namespace Next.Accounts_Client.Controllers
{
    public interface IUsingTracker
    {
        void StartTimer();

        void StopTimer();

        void SetAccount(Accounts_Server.Models.Account account);

        void ClearAccount();
    }
}