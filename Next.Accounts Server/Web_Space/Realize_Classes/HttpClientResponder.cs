﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space.Realize_Classes
{
    public class HttpClientResponder : IHttpProcessor, IDisposable
    {

        public  IEventListener EventListener { get; set; }

        public IDatabase Database { get; set; }

        private readonly DispatcherTimer _timer;

        public IUsedTracker UsedTracker { get; set; }

        private readonly Sender _me;

        private Settings _settings;

        public HttpClientResponder(Sender me, Settings settings, int min = 1)
        {
            _me = me;
            _settings = settings;
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
            EventListener.OnEvent(text);
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
                var apiMessage = request.PostData.ParseJson<ApiMessage>();
                var requestType = Const.GetRequestType(apiMessage);
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
            else if (request.HttpMethod == "GET")
            {
                var html = await GetHtmlPage(context) ??
                           "<h1>Infopage.html does not exists</h1><h2>Load it from github</h2>";
                CloseHttpContext(context, html, contentType: "text/html");
                EventListener.OnEvent("GET request has been processed");
            }
        }

        private async Task<string> GetHtmlPage(HttpListenerContext context)
        {
            var html = await IoController.ReadFileAsync(Const.HtmlPageFilename);
            var accounts = await Database.GetAccounts();
            if (html == null) return null;
            var meText = $"Information about server:" +
                         $"<ul>" +
                         $"<li>Version: {_me.AppVersion}</li>" +
                         $"<li>Machine name: {_me.Name}</li>" +
                         $"<li>Local IP: {_me.IpAddress}</li>" +
                         $"</ul>";
            //html = html.Replace("#CenterName", _settings.CenterName).Replace("#me", meText);
            string accountList = "No accounts in local storage (Null data)";
            if (accounts != null)
            {
                accountList = "No accounts in local storage";
                if (accounts.Count > 0)
                {
                    accountList = accounts.Aggregate("<ul class=\"list-group\">",
                    (current, a) => current + $"<li class=\"list-group-item\">{a}</li>");
                }
                //html.Replace("#AccountList", accountList);
            }
            //html = html.Replace("#AccountList", accountList).Replace("#CenterName", _settings.CenterName).Replace("#me", meText);
            var request = context.Request;
            var senderText = $"Request data:<br>" +
                             $"HttpMethod: {request.HttpMethod}<br>" +
                             $"End point: {request.RemoteEndPoint?.Address.ToString()}:{request.RemoteEndPoint?.Port}<br>" +
                             $"User agent: {request.UserAgent}<br>" +
                             $"Raw url: {request.RawUrl}";
            html = html
                .Replace("#sender", senderText)
                .Replace("#AccountList", accountList)
                .Replace("#CenterName", _settings.CenterName)
                .Replace("#me", meText);
            return html;
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
                    response.RequestType = Const.RequestTypeGet;
                    var usedCount = UsedTracker.GetUsedCount();
                    if (accountToSend == null || _settings.SetIssueLimit && usedCount >= _settings.IssueLimitValue)
                    {
                        response.JsonObject = null;
                        response.Code = 404;
                        response.StringMessage = "No accounts available";
                        messageToDisplay = $"Request from {sender} has been denied";
                    }
                    else
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
                    break;

                case ApiRequests.ReleaseAccount:
                    response.RequestType = Const.RequestTypeRelease;
                    response.JsonObject = null;
                    if (apiRequest.JsonObject != "null")
                    {
                        var releaseAccount = apiRequest.JsonObject?.ParseJson<Account>();
                        int result = await Database.ReleaseAccount(releaseAccount);
                        response.Code = 200;
                        response.JsonObject = releaseAccount.ToJson();
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
                    response.RequestType = Const.RequestTypeUsing;
                    response.JsonObject = null;
                    var usingAccount = apiRequest.JsonObject?.ParseJson<Account>();
                    if (usingAccount != null)
                    {
                        var resetResult = UsedTracker.ResetTimer(usingAccount);
                        if (resetResult)
                        {
                            response.Code = 200;
                            response.JsonObject = usingAccount.ToJson();
                            response.StringMessage = "Account time has been reset";
                        }
                        else
                        {
                            response.Code = 404;
                            response.StringMessage = "Account time has not been reset";
                            messageToDisplay = $"Could not reset time of account {usingAccount}";
                        }
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
            if (messageToDisplay != null) EventListener.OnEvent(messageToDisplay);
            return response;
        }

        private void CloseHttpContext(HttpListenerContext context, ApiMessage response,
            HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/json; charset=UTF-8")
        {
            CloseHttpContext(context, response.ToJson(), code, contentType);
        }

        private void CloseHttpContext(HttpListenerContext context, string response, HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/json; charset=UTF-8")
        {
            context.Response.ContentType = contentType;
            context.Response.ContentEncoding = Encoding.ASCII;
            var buffer = response.ToBuffer();
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = (int) code;
            //EventListener.OnEvent($"Processed client: " +
            //                         $"{context.Request.LocalEndPoint?.Address}:{context.Request.LocalEndPoint?.Port}");
            try
            {
                context.Response.Close(buffer, false);
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 1229) return;
                EventListener.OnException(ex);
            }
            catch (Exception ex) { EventListener.OnException(ex);}
        }

        

        public void Dispose()
        {
            _timer?.Stop();
        }
    }
}