using System;
using System.Net.Sockets;
using System.Text;

namespace Next.Accounts_Server.Extensions
{
    public static class TcpExtensions
    {
        public static bool Respond(this TcpClient client, string response)
        {
            try
            {
                var buffer = response.ToBuffer();
                client.GetStream().Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception ex)
            {
                // ignored
            }
            return false;
        }

        public static string GetClientRequest(this TcpClient client, int length = 4096)
        {
            var buffer = new byte[1024];
            int count;
            var request = "";
            while ((count = client.GetStream().Read(buffer, 0, buffer.Length)) > 0)
            {
                request += Encoding.ASCII.GetString(buffer, 0, count);
                if (request.IndexOf("\r\n\r\n") >= 0 || request.Length > length)
                {
                    break;
                }
            }
            return request;
        }
    }
}