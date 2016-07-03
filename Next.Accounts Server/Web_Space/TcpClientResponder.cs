using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;

namespace Next.Accounts_Server.Web_Space
{
    public class TcpClientResponder : ITcpListener
    {
        private readonly IEventListener _eventListener;

        public TcpClientResponder(IEventListener eventListener)
        {
            _eventListener = eventListener;
        }

        public void OnClientConnected(TcpClient client)
        {
            var request = client.GetClientRequest();

            client.Respond(request);
            client.Close();
            //SendError(client, 200);
        }

        private void SendError(TcpClient client, int code, string resp = null )
        {
            var codeStr = $"{code} {(HttpStatusCode)code}";
            var response = $"HTTP/1.1 {codeStr}\n" +
                           $"Content-type: text/json\n" +
                           $"Content-Length: {codeStr.Length}\n\n";
            if (!string.IsNullOrEmpty(resp))
            {
                response += resp;
            }

            client.Respond(response);
            client.Close();
        }
    }
}