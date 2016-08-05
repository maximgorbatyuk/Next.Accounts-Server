using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Next.Accounts_Server.Models
{
    public class HttpRequest
    {
        public string HttpMethod { get; set; }

        public IDictionary<string, IEnumerable<string>> Headers { get; set; }

        public Stream InputStream { get; set; }

        public int ContentLength => int.Parse(Headers["Content-Length"].First());

        public string RawUrl { get; set; }

        public string PostData { get; set; } = null;

        public string SenderAddress { get; set; }

        public override string ToString()
        {
            return $"Sender={SenderAddress}, HttpMethod={HttpMethod}, ContentLength={ContentLength}, RawUrl={RawUrl}";
        }
    }
}