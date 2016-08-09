namespace Next.Accounts_Client.Controllers
{
    public interface ITrackerListener
    {
        void OnAccountReleased(Accounts_Server.Models.Account account);

        void OnSteamStarted();

        void OnSteamClosed();
    }
}