namespace Next.Accounts_Server.Web_Space.Model
{
    public class ApiMessage
    {
        public int Code { get; set; } = 200;

        public string RequestType { get; set; } = null;

        public string JsonObject { get; set; } = null;

        public string JsonSender { get; set; } = null;

        public string StringMessage { get; set; } = null;

        public bool VacBanFree { get; set; } = false;

        public override string ToString()
        {
            var jsonObject = JsonObject ?? "null";
            var jsonSender = JsonSender ?? "null";
            var stringMessage = StringMessage ?? "null";
            var requestType = RequestType ?? "null";
            return $"Api message (code: {Code}, type: {requestType}, object: {jsonObject}, sender: {jsonSender}, message: {stringMessage})";
        }
    }
}