using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Next.Accounts_Server.Web_Space
{
    public class WebServer
    {
        private readonly IWebListener _listener;
        private readonly string _url;
        private readonly HttpListener _server;
        private readonly int _threadCount;
        private List<Thread> _threads;

        public WebServer(IWebListener listener, string url = "http://*:8080/", int count = 5)
        {
            _listener = listener;
            _server = new HttpListener();
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
                    _listener.OnWebSystemMessage(
                        $"netsh http add urlacl url={_url} user={userdomain}\\{username} listen=yes");
                }
                _listener.OnWebError(ex);
                return;
            }


            _threads = new List<Thread>();
            for (var i = 0; i < _threadCount; i++)
            {
                var thread = new Thread(Listenning);
                _threads.Add(thread);
                thread.Start();
            }
            _listener.OnWebSystemMessage("Listenning started");
        }

        public void Close()
        {
            _server.Stop();
            if (_threads == null) return;
            foreach (var thread in _threads)
            {
                thread.Abort();
            }
            _listener.OnWebSystemMessage("Listenning stopped");
        }

        public void Listenning()
        {
            while (_server.IsListening)
            {
                try
                {
                    HttpListenerContext context = _server.GetContext();

                    //получаем входящий запрос
                    HttpListenerRequest request = context.Request;
                    _listener.OnRequestReceived(request);
                    

                    var responseDic = new Dictionary<string, string>
                    {
                        {"response", context.Response.StatusCode.ToString() },
                        {"raw_url", request.RawUrl },
                        {"method", request.HttpMethod },
                        {"user_agent", request.UserAgent }
                    };

                    var jsonParser = new JsonParser();

                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/plain; charset=UTF-8";
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonParser.ToJson(responseDic));
                    response.ContentLength64 = buffer.Length;

                    using (Stream output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
                catch (Exception ex)
                {
                    _listener.OnWebError(ex);
                }
            }
        }


    }
}