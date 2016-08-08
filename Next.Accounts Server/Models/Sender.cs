using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Server.Models
{
    public class Sender
    {
        public string IpAddress { get; set; } = "";

        public string Name { get; set; } = Const.NoName;

        public string AppType { get; set; } = Const.ClientAppType;

        public string AppVersion { get; set; } = "0.0.0.0";

        public override string ToString()
        {
            return $"{Name} (type {AppType}, ip {IpAddress}, version {AppVersion})";
        }
    }
}