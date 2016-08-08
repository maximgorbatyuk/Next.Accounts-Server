using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Interfaces;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space
{
    public class HttpClientResponder : IHttpProcessor, IDisposable
    {

        public  IEventListener EventListener { get; set; }

        public IDatabase Database { get; set; }

        private readonly DispatcherTimer _timer;

        public IUsedTracker UsedTracker { get; set; }

        private readonly Sender _me;

        public HttpClientResponder(Sender me, int min = 1)
        {
            _me = me;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(min) };
            _timer.Tick += UsedTrackerTimerTick;
            _timer.Start();
        }

        private void UsedTrackerTimerTick(object sender, EventArgs eventArgs)
        {
            var exspired = UsedTracker.ClearUpUsed();
            UsedTracker.IncreaseTime();
            if (exspired == null) return;

            var text = "Released next accounts with expired \"using\" time by server: \r\n";
            for (int index = 0; index < exspired.Count; index++)
            {
                var account = exspired[index];
                Database.ReleaseAccount(account);
                text += $"{index + 1}) account {account})\r\n";
            }
            EventListener.OnMessage(text);
        }


        public async void OnRequestReceived(HttpListenerContext context)
        {
            var sender = context.Request.UserHostAddress;
            var request = new HttpRequest
            {
                HttpMethod = context.Request.HttpMethod,
                Headers = context.Request.Headers.ToDictionary(),
                InputStream = context.Request.InputStream,
                RawUrl = context.Request.RawUrl,
                SenderAddress = context.Request.UserHostAddress
            };

            //ReturnWebError(context, "Fuck you");
            //return;

            if (request.HttpMethod == "POST")
            {
                request.PostData = new StreamReader(request.InputStream).ReadToEnd();
            }

            var apiMessage = request.PostData.ParseJson<ApiMessage>();
            var requestType = GetRequestType(apiMessage);
            var response = await CreateHttpResponseAsync(requestType, request);
            if (response != null)
            {
                CloseHttpContext(context, response);
            }
            else
            {
                ReturnWebError(context, "Server could not recognize received request");
            }
        }

        public void ReturnWebError(HttpListenerContext context, string message)
        {
            var api = new ApiMessage
            {
                Code = 404,
                JsonObject = null,
                RequestType = "Error",
                StringMessage = message,
                JsonSender = _me.ToJson()
            };
            CloseHttpContext(context, api, HttpStatusCode.NotFound);
        }

        private async Task<ApiMessage> CreateHttpResponseAsync(ApiRequests type, HttpRequest request)
        {
            var apiRequest = request.PostData.ParseJson<ApiMessage>();
            var response = new ApiMessage
            {
                Code = 200,
                JsonObject = null,
                RequestType = null,
                StringMessage = request.HttpMethod == "POST" ? request.PostData : request.RawUrl,
                JsonSender = _me.ToJson()
            };

            var sender = apiRequest.JsonSender.ParseJson<Sender>();
            string messageToDisplay = null;
            switch (type)
            {
                case ApiRequests.GetAccount:
                    var accountToSend = await Database.GetAccount(sender);
                    response.RequestType = "GetAccount";

                    if (accountToSend != null)
                    {
                        response.JsonObject = accountToSend.ToJson();
                        response.Code = 200;
                        response.StringMessage = "AvailableAccount";
                        if (sender.AppType == Const.ClientAppType)
                        {
                            UsedTracker.AddAccount(accountToSend);
                        }
                        messageToDisplay = $"Account {accountToSend} has been sent to {sender}";
                    }
                    else
                    {
                        response.JsonObject = null;
                        response.Code = 404;
                        response.StringMessage = "No accounts available";
                        messageToDisplay = $"Request from {sender} has been denied";
                    }
                    break;

                case ApiRequests.ReleaseAccount:
                    response.RequestType = "ReleaseAccount";
                    response.JsonObject = null;
                    if (apiRequest.JsonObject != "null")
                    {
                        var releaseAccount = apiRequest.JsonObject?.ParseJson<Account>();
                        int result = await Database.ReleaseAccount(releaseAccount);
                        response.Code = 200;
                        response.StringMessage = "Account has been released";
                        var releaseResult = false;
                        if (sender.AppType == Const.ClientAppType)
                        {
                            releaseResult = UsedTracker.RemoveAccount(releaseAccount);
                        }
                        messageToDisplay = releaseResult ? 
                            $"Account {releaseAccount}({result}) has been released. Sender {sender}" :
                            $"Got a request to release an account: {releaseAccount} from {sender}, but it has been released earier";
                    }
                    else
                    {
                        response.Code = 404;
                        response.StringMessage = "Account has not been released because of null data";
                        messageToDisplay = $"Received release request with null data from {sender}";
                    }

                    break;
                case ApiRequests.UsingAccount:
                    response.RequestType = "UsingAccount";
                    var usingAccount = apiRequest.JsonSender?.ParseJson<Account>();
                    if (usingAccount != null)
                    {
                        var resetResult = UsedTracker.ResetTimer(usingAccount);
                        //resetResult
                        if (!resetResult) messageToDisplay = $"Could not reset time of account {usingAccount}";
                    }
                    else
                    {
                        messageToDisplay = $"Server has received a messsage from {sender} about using of account, " +
                                           $"but List of used does not contain it. Message: {apiRequest}";
                    }
                    break;
                case ApiRequests.Unknown:
                case ApiRequests.None:
                default:
                    response = null;
                    break;
            }
            if (messageToDisplay != null) EventListener.OnMessage(messageToDisplay);
            return response;
        }

        private void CloseHttpContext(HttpListenerContext context, ApiMessage response, HttpStatusCode code = HttpStatusCode.OK)
        {
            context.Response.ContentType = "text/json; charset=UTF-8";
            context.Response.ContentEncoding = Encoding.ASCII;
            var buffer = response.ToJson().ToBuffer();
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = (int) code;
            EventListener.OnMessage($"Processed client: " +
                                     $"{context.Request.LocalEndPoint?.Address}:{context.Request.LocalEndPoint?.Port}");
            context.Response.Close(buffer, false);
        }

        private ApiRequests GetRequestType(ApiMessage api)
        {
            var result = ApiRequests.None;
            if (api.RequestType == null) return result;
            switch (api.RequestType)
            {
                case "GetAccount":
                    result = ApiRequests.GetAccount;
                    break;
                case "ReleaseAccount":
                    result = ApiRequests.ReleaseAccount;
                    break;
                default:
                    result = ApiRequests.Unknown;
                    break;
            }
            return result;
        }

        public void Dispose()
        {
            _timer?.Stop();
        }
    }
}