using System;
using System.Net.Sockets;

namespace Next.Accounts_Server.Web_Space
{
    public interface ITcpListener
    {
        void OnClientConnected(TcpClient client);
    }
}