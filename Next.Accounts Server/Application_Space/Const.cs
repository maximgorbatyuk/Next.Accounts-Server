using System.Collections.Generic;
using System.Net;

namespace Next.Accounts_Server.Application_Space
{
    public class Const
    {
        public static readonly string NoLogin = "NoLogin";

        public static readonly string NoPassword = "NoPassword";

        public static readonly string NoName = "NoName";

        public static readonly string IdColumn = "Id";

        public static readonly string LoginColumn = "Login";

        public static readonly string PasswordColumn = "Password";

        public static readonly string AvailableColumn = "Available";

        public static readonly string ComputerNameColumn = "ComputerName";

        public static readonly string NoComputer = "NoComputer";

        public static readonly string CenterOwnerColumn = "Owner";

        public static readonly string SettingsFilename = "settings.js";

        public static readonly string ClientAppType = "Client";

        public static readonly string ServerAppType = "Server";

        public static IEnumerable<IPAddress> GetAddresses()
        {
            var me = Dns.GetHostName();
            var addresses = Dns.GetHostEntry(me).AddressList;
            return addresses;
        }
    }
}