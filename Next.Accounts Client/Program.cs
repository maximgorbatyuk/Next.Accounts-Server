using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Next.Accounts_Client
{
    static class Program
    {
        private static Mutex _mutex;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string windowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIonoc(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int showWindowCommand);


        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool created;
            _mutex = new Mutex(true, "next-kz-steam-tracker", out created);
            if (!created)
            {
                IntPtr hWnd = FindWindow(null, "steam");
                if (IsIonoc(hWnd)) ShowWindow(hWnd, 9);
                else SetForegroundWindow(hWnd);
                return;
            }
            Application.Run(new Form1(args));
        }
    }
}
