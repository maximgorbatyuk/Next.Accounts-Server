using System;
using System.Net;
using System.Threading.Tasks;
using Next.Accounts_Server.Extensions;

namespace Next.Accounts_Client.Web_Space
{
    public class WebClientController
    {
        private WebClient _client;

        private Accounts_Server.Application_Space.IEventListener _listener;

        private string _url;

        public WebClientController(
            Accounts_Server.Application_Space.IEventListener listener, 
            string url = @"http://localhost:8082")
        {
            _listener = listener;
            _url = url;
            _client = new WebClient();
        }

        private void ClientOnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs downloadStringCompletedEventArgs)
        {
            var response = downloadStringCompletedEventArgs.Result;
            _listener.OnMessage(response);
        }

        public async Task<string> SendPostDataAsync(Accounts_Server.Web_Space.Model.ApiMessage message)
        {
            var toSend = message.ToJson();
            //var uri = new Uri(_url);
            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string result = null;
            try
            {
                result = await _client.UploadStringTaskAsync(_url, toSend);
            }
            catch (Exception ex)
            {
                _listener.OnException(ex);
            }
            return result;
        }
    }
}