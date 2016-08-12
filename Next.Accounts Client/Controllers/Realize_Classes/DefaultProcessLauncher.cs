using System;
using System.Diagnostics;
using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Client.Controllers.Realize_Classes
{
    public class DefaultProcessLauncher : IProcessLauncher
    {
        private readonly IEventListener _listener;

        public DefaultProcessLauncher(IEventListener listener)
        {
            _listener = listener;
        }

        public bool StartProcess(string processName)
        {
            try
            {
                var p = Process.Start(processName);
                return p != null;
            }
            catch (Exception ex)
            {
                _listener.OnException(ex);
            }
            return false;
        }

        public bool IsActive(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                _listener.OnException(ex);
            }
            return false;
        }

        public bool StartProcess(ProcessStartInfo info)
        {

            try
            {
                var p = Process.Start(info);
                return p != null;
            }
            catch (Exception ex)
            {
                _listener.OnException(ex);
            }
            return false;
        }

        public bool CloseProcesses(string processname)
        {
            var processes = Process.GetProcessesByName(processname);
            if (processes.Length == 0) return false;
            foreach (var p in processes)
            {
                p.Kill();
            }
            return true;
        }

        public void KillMe()
        {
            var me = Process.GetCurrentProcess();
            me.Kill();
        }
    }
}