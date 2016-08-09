using System;
using System.Net;
using System.Threading.Tasks;
using Next.Accounts_Client;
using Next.Accounts_Client.Controllers;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;

namespace Next.Accounts_Client.Web_Space
{
    public class WebClientController : IRequestSender
    {
        private WebClient _client;

        private IListener _listener;

        private IResponseListener _responseListener;

        private App_data.Settings _settings;

        private string _url;

        public WebClientController(IListener listener, IResponseListener responseListener, string url = @"http://localhost:8082")
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

        public async Task<bool> SendPostDataAsync(Accounts_Server.Web_Space.Model.ApiMessage message)
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