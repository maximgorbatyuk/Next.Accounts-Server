﻿using System;
using System.Net;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space.Realize_Classes
{
    public class WebClientController : IRequestSender
    {
        private readonly WebClient _client;

        private readonly IEventListener _listener;

        private readonly IResponseListener _responseListener;

        //private  CLientSettings _settings;

        private string _url;

        public WebClientController(IEventListener listener, IResponseListener responseListener, string url = @"http://localhost:8082")
        {
            _listener = listener;
            _responseListener = responseListener;
            _url = url;
            _client = new WebClient();
        }

        private void ClientOnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs downloadStringCompletedEventArgs)
        {
            var response = downloadStringCompletedEventArgs.Result;
            _listener.OnEvent(response);
        }

        public async Task<bool> SendPostDataAsync(ApiMessage message)
        {
            var toSend = message.ToJson();
            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            try
            {
                var result = await _client.UploadStringTaskAsync(_url, toSend);
                _responseListener.OnServerResponse(result);
                return true;
            }
            catch (Exception ex)
            {
                _listener.OnException(ex);
                _responseListener.OnConnectionError(ex);
            }
            return false;
        }
    }
}