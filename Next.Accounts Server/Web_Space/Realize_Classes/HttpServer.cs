using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Server.Web_Space.Realize_Classes
{
    public class HttpServer
    {
        private readonly IHttpProcessor _processor;
        private readonly IEventListener _eventListener;
        private readonly string _url;
        private HttpListener _server;
        private readonly int _threadCount;
        private List<Thread> _threads;
        private string _prefix;
        private bool _activeStatement = false;

        public HttpServer(IHttpProcessor processor, IEventListener eventListener, string url = "http://+:8082/", int count = 5)
        {
            
            _processor = processor;
            _eventListener = eventListener;
            _prefix = url;
            _threadCount = count;
            _url = url;
        }

        public bool GetListenState() => _server?.IsListening ?? false;

        public void Start()
        {
            Close();
            //if (_server != null && _server.IsListening) Close();

            try
            {
                _server = new HttpListener();
                _server.Prefixes.Add(_prefix);
                //_server.Prefixes.Add("http://192.168.1.100:8082/");
                // "http://192.168.1.100:8082/"
                _server.TimeoutManager.RequestQueue = TimeSpan.FromSeconds(10);
                _server.Start();
                _activeStatement = true;
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    var username = Environment.GetEnvironmentVariable("USERNAME");
                    var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                    // netsh http add urlacl url=http://*:9669/ user=fak listen=yes
                    _eventListener.OnEvent(
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
            _eventListener.OnEvent("Listenning started");
        }

        public void Close()
        {
            if (_server != null && _server.IsListening) _server?.Stop();
            if (_threads == null) return;
            _activeStatement = false;
            foreach (var thread in _threads)
            {
                thread.Abort();
            }
            _threads = null;
            _eventListener.OnEvent("Listenning stopped");
        }

        public void Listenning()
        {
            while (_activeStatement)
            {
                try
                {
                    HttpListenerContext context = _server.GetContext();
                    _processor.OnRequestReceived(context);
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Операция ввода/вывода была прервана из-за завершения потока команд или по запросу приложения" ||
                        ex.Message == "Поток находился в процессе прерывания.") continue;
                    _eventListener.OnException(ex);
                }
            }
        }


    }
}