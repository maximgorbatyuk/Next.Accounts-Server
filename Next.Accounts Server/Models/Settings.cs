namespace Next.Accounts_Server.Models
{
    public class Settings
    {
        public string DatabaseName { get; set; } = "SteamAccounts.db3";

        public string PostgresConnectionString { get; set; } = "Host=Off.ala.next.kz;Port=5432;User ID=postgres;Password=AsusNotebook;Database=postgres;";

        public string CenterName { get; set; } = "Unknown";

        public bool AskAccounts { get; set; } = false;

        public bool GiveAccounts { get; set; } = false;

        public bool SetIssueLimit { get; set; } = false;

        public int IssueLimitValue { get; set; } = 10;

        public bool DefaultSettings { get; set; } = true;

        public int UsedMinuteLimit { get; set; } = 5;
    }
}