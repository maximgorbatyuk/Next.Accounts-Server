namespace Next.Accounts_Server.Controllers
{
    public interface ILogger
    {
        void Log(string message);

        void LogError(string error);

        void LogAccountUsement(string message);
    }
}