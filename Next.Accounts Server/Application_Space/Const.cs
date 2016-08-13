using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Model;

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

        public static readonly string RequestTypeUsing = "UsingAccount";

        public static readonly string RequestTypeGet = "GetAccount";

        public static readonly string RequestTypeRelease = "ReleaseAccount";

        public static readonly string HtmlPageFilename = "infopage.html";

        

        public static IEnumerable<IPAddress> GetAddresses()
        {
            var me = Dns.GetHostName();
            var addresses = Dns.GetHostEntry(me).AddressList;
            var localAddresses = addresses.Where(a => a.ToString().Contains("192.168"));
            return localAddresses;
        }

        public static ApiRequests GetRequestType(ApiMessage api)
        {
            var result = ApiRequests.None;
            if (api.RequestType == null) return result;
            switch (api.RequestType)
            {
                case "GetAccount":
                    result = ApiRequests.GetAccount;
                    break;
                case "ReleaseAccount":
                    result = ApiRequests.ReleaseAccount;
                    break;

                case "UsingAccount":
                    result = ApiRequests.UsingAccount;
                    break;
                default:
                    result = ApiRequests.Unknown;
                    break;
            }
            return result;
        }

        public static Sender GetSender(string name = null,  string version = null, bool client = true)
        {
            if (version == null) version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var ip = Const.GetAddresses().SingleOrDefault(a => a.ToString().Contains("192.168.1."));
            var stringIp = ip?.ToString() ?? "null ip";
            var sender = new Sender
            {
                AppVersion = version,
                AppType = client ? Const.ClientAppType : Const.ServerAppType,
                IpAddress = stringIp,
                Name = name ?? Environment.MachineName
            };
            return sender;
        }

        public static int GetRandomNumber(int max, int min = 0)
        {
            var random = new Random(DateTime.Now.Millisecond);
            var result = random.Next(min, max);
            return result;
        }
    }
}