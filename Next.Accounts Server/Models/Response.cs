namespace Next.Accounts_Server.Models
{
    public class Response
    {
        public int Code { get; set; } = 200; // OK

        public string Type { get; set; } = null;

        public string Result { get; set; } = null;

        public string Request { get; set; } = null;
    }
}