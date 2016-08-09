using System.Diagnostics;

namespace Next.Accounts_Client.Controllers
{
    public interface IProcessLauncher
    {
        bool IsActive(string processName);

        bool StartProcess(string processName);

        bool StartProcess(ProcessStartInfo info);

        bool CloseProcesses(string processname);

        void KillMe();

    }
}