namespace Next.Accounts_Server.Web_Space.Model
{
    public class ApiMessage
    {
        public int Code { get; set; } = 200;

        public string RequestType { get; set; } = null;

        public string JsonObject { get; set; } = null;

        public string JsonSender { get; set; } = null;

        public string StringMessage { get; set; } = null;
    }
}