using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;

namespace Next.Accounts_Server.Web_Space
{
    public class HttpServer
    {
        private readonly IHttpListener _listener;
        private readonly IEventListener _eventListener;
        private readonly string _url;
        private readonly System.Net.HttpListener _server;
        private readonly int _threadCount;
        private List<Thread> _threads;

        public HttpServer(IHttpListener listener, IEventListener eventListener, string url = "http://*:8080/", int count = 5)
        {
            _listener = listener;
            _eventListener = eventListener;
            _server = new System.Net.HttpListener();
            _server.Prefixes.Add(url);
            _threadCount = count;
            _url = url;
        }

        public bool GetListenState() => _server?.IsListening ?? false;

        public void Start()
        {
            if (_server.IsListening)
            {
                Close();
            }

            try
            {
                _server.Start();
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    var username = Environment.GetEnvironmentVariable("USERNAME");
                    var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                    // netsh http add urlacl url=http://*:9669/ user=fak listen=yes
                    _eventListener.OnMessage(
                        $"netsh http add urlacl url={_url} user={userdomain}\\{username} listen=yes");
                }
                _eventListener.OnException(ex);
                return;
            }


            _threads = new List<Thread>();
            for (var i = 0; i < _threadCount; i++)
            {
                var thread = new Thread(Listenning);
                _threads.Add(thread);
                thread.Start();
            }
            _eventListener.OnMessage("Listenning started");
        }

        public void Close()
        {
            _server.Stop();
            if (_threads == null) return;
            foreach (var thread in _threads)
            {
                thread.Abort();
            }
            _eventListener.OnMessage("Listenning stopped");
        }

        public void Listenning()
        {
            while (_server.IsListening)
            {
                try
                {
                    HttpListenerContext context = _server.GetContext();
                    _listener.OnRequestReceived(context);
                }
                catch (Exception ex)
                {
                    _eventListener.OnException(ex);
                }
            }
        }


    }
}