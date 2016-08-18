using System;
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
        public IServerSpeaker ServerSpeaker { get; set; }
        public IGetResponder GetResponder { get; set; }
        public IUsedTracker UsedTracker { get; set; }
        public ISettingsChangedListener SettingsChangedListener { get; set; }

        private readonly DispatcherTimer _timer;

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
            var expired = UsedTracker.ClearUpUsed();
            UsedTracker.IncreaseTime();
            if (expired == null) return;

            var text = "Released next accounts with expired \"using\" time by server: \r\n";
            for (int index = 0; index < expired.Count; index++)
            {
                var account = expired[index];
                Database.ReleaseAccount(account);
                text += $"{index + 1}) account {account})\r\n";
            }
            EventListener.OnEvent(text);
        }


        public async void OnRequestReceived(HttpListenerContext context)
        {
            //var sender = context.Request.UserHostAddress;
            var request = new HttpRequest
            {
                HttpMethod = context.Request.HttpMethod,
                Headers = context.Request.Headers.ToDictionary(),
                InputStream = context.Request.InputStream,
                RawUrl = context.Request.RawUrl,
                SenderAddress = context.Request.UserHostAddress
            };
            string remoteIp = $"{context.Request.RemoteEndPoint?.Address.ToString()}:{context.Request.RemoteEndPoint?.Port}";
            //ReturnWebError(context, "Fuck you");
            //return;

            switch (request.HttpMethod)
            {
                case "POST":
                    request.PostData = new StreamReader(request.InputStream).ReadToEnd();
                    if (request.RawUrl == "/settings")
                    {
                        // %3A
                        request.PostData = request.PostData.Replace("%3A", ":").Replace("%0D%0A", "\n");
                        string htmlSettings = null;
                        var postArray = request.PostData.Split('&').ToList();
                        try
                        {
                            var postgresConnString =
                                postArray.FirstOrDefault(p => p.Contains("PostgresConnectionString"))?.Split('=')[1];
                            var minimal = postArray.FirstOrDefault(p => p.Contains("MinimalAccountLimit"))?.Split('=')[1].ParseInt();

                            var addressesList =
                                postArray.FirstOrDefault(p => p.Contains("AddressesList"))?.Split('=')[1].Split('\n').ToList();
                            var askAccounts = postArray.FirstOrDefault(p => p.Contains("AskAccounts"))?.Split('=')[1] ==
                                              "on";
                            var giveAccounts =
                                postArray.FirstOrDefault(p => p.Contains("GiveAccounts"))?.Split('=')[1] == "on";
                            var setIssueLimit =
                                postArray.FirstOrDefault(p => p.Contains("SetIssueLimit"))?.Split('=')[1] == "on";
                            var issueLimitValue =
                                postArray.FirstOrDefault(p => p.Contains("IssueLimitValue"))?.Split('=')[1].ParseInt();
                            var usedMinuteLimit =
                                postArray.FirstOrDefault(p => p.Contains("UsedMinuteLimit"))?.Split('=')[1].ParseInt();
                            _settings.PostgresConnectionString = string.IsNullOrWhiteSpace(postgresConnString)
                                ? _settings.PostgresConnectionString
                                : postgresConnString;
                            _settings.AddressesList = addressesList?.Where(a => string.IsNullOrWhiteSpace(a) == false).ToList();
                            _settings.AskAccounts = askAccounts;
                            _settings.GiveAccounts = giveAccounts;
                            _settings.SetIssueLimit = setIssueLimit;
                            if (issueLimitValue != null)
                            {
                                _settings.IssueLimitValue = issueLimitValue.Value == -1
                               ? _settings.IssueLimitValue :
                               issueLimitValue.Value;
                            }
                            if (usedMinuteLimit != null)
                            {
                                _settings.UsedMinuteLimit = usedMinuteLimit.Value == -1
                               ? _settings.UsedMinuteLimit :
                               usedMinuteLimit.Value;
                            }
                            if (minimal != null)
                            {
                                _settings.MinimalAccountLimit = minimal.Value == -1
                               ? _settings.MinimalAccountLimit :
                               minimal.Value;
                            }

                            htmlSettings = await GetResponder.GetHtmlPage(context, raw:"/settings", message: $"Settings has been updated");
                            SettingsChangedListener.OnSettingsChanged(_settings);
                           
                        }
                        catch (Exception ex)
                        {
                            EventListener.OnException(ex);
                            
                            htmlSettings = await GetResponder.GetHtmlPage(context, $"Could not save settings. Exception {ex.Message}", error: true);
                        }
                        //htmlSettings = postArray.ToJson();

                        //var htmlPost = await GetResponder.GetHtmlPage(context);
                        CloseHttpContext(context, htmlSettings, contentType: "text/html");
                        EventListener.OnEvent($"Updated settings by request from {remoteIp}");
                        
                    }
                    else
                    {
                        
                        var apiMessage = request.PostData.ParseJson<ApiMessage>();
                        var requestType = Const.GetRequestType(apiMessage);
                        var sender = apiMessage.JsonSender.ParseJson<Sender>();
                        ApiMessage response = null;

                        if (sender?.AppType == Const.ClientAppType)
                            response = await CreateHttpResponseAsync(requestType, request);

                        else if (sender?.AppType == Const.ServerAppType)
                            response = await ServerSpeaker.CreateResponseForRequester(requestType, sender, _me, apiMessage);

                        if (response != null) CloseHttpContext(context, response);
                        else ReturnWebError(context, "Server could not recognize received request");
                    }
                    break;

                case "GET":
                    var html = await GetResponder.GetHtmlPage(context) ??
                               "<div class=\"container\">" +
                               "<h1>Infopage.html does not exists</h1><br><br><br>" +
                               "<h2>Load it from github or ask a <a href='https://new.vk.com/maximgorbatyuk'>developer</a> for it</h2>";
                    CloseHttpContext(context, html, contentType: "text/html");
                    EventListener.OnEvent($"GET request({remoteIp}) has been processed");
                    break;
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
                    if (accountToSend == null)
                    {
                        await ServerSpeaker.AskAccounts(_me);
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