namespace Next.Accounts_Server.Web_Space.Model
{
    public class ApiMessage
    {
        public int Code { get; set; }

        public string Type { get; set; }

        public string RequestType { get; set; }

        public string JsonObject { get; set; }

        public string SenderDescription { get; set; }
    }
}