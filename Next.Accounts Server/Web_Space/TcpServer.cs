using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Server.Web_Space
{
    public class TcpServer
    {
        private readonly TcpListener _server;
        private readonly ITcpListener _listener;
        private readonly IEventListener _eventListener;
        private bool _active;
        private readonly List<Thread> _threads;
        private readonly int _threadsCount;

        public TcpServer(ITcpListener listener, IEventListener eventListener, int port = 8080, int count = 5)
        {
            _listener = listener;
            _eventListener = eventListener;
            _server = new TcpListener(IPAddress.Any, port);
            _threads = new List<Thread>();
            _threadsCount = count;
        }

        public void Start()
        {
            if (_active)
            {
                _server.Stop();
            }
            _server.Start();
            _active = true;
            for (var i = 0; i < _threadsCount; i++)
            {
                var th = new Thread(Listenning);
                th.Start();
                _threads.Add(th);
            }
        }

        private void Listenning()
        {
            while (_active)
            {
                try
                {
                    var client = _server.AcceptTcpClient();
                    _listener.OnClientConnected(client);
                }
                catch (Exception ex)
                {
                    // ignored
                }
            }
        }

        public void Stop()
        {
            _active = false;
            _server?.Stop();
            foreach (var th in _threads)
            {
                th.Abort();
            }
            _threads.Clear();
        }


        ~TcpServer()
        {
            Stop();
        }
    }
}