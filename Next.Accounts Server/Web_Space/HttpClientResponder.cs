using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Interfaces;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space
{
    public class HttpClientResponder : IHttpProcessor
    {

        private readonly IEventListener _eventListener;
        private readonly IDatabase _database;
        private readonly Sender _me;

        public HttpClientResponder(IEventListener eventListener, IDatabase database, Sender me)
        {
            _eventListener = eventListener;
            _database = database;
            _me = me;
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
            CloseHttpContext(context, response);
        }

        public void ReturnWebError(HttpListenerContext context, string message, int code = 404)
        {
            var api = new ApiMessage
            {
                Code = 404,
                JsonObject = message,
                RequestType = "Error",
                StringMessage = message
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
            switch (type)
            {
                case ApiRequests.GetAccount:
                    var sender = apiRequest.JsonSender.ParseJson<Sender>();
                    var accountToSend = await _database.GetAccount(sender);
                    response.JsonObject = accountToSend != null ? accountToSend.ToJson() : null;
                    response.Code = accountToSend != null ? 200 : 404;
                    response.RequestType = "GetAccount";
                    response.StringMessage = accountToSend != null ? "" : "No accounts available";
                    break;
                case ApiRequests.ReleaseAccount:
                    if (apiRequest.JsonObject != "null")
                    {
                        var releaseAccount = apiRequest.JsonObject.ParseJson<Account>();
                        int result = await _database.ReleaseAccount(releaseAccount);
                        response.JsonObject = null;
                        response.Code = 200;
                        response.RequestType = "ReleaseAccount";
                        response.StringMessage = "Account has been released";
                    }
                    else
                    {
                        response.Code = 404;
                        response.RequestType = "ReleaseAccount";
                        response.StringMessage = "Account has not been released because of null data";
                    }
                    
                    break;
                default:
                    response.Code = 404;
                    response.RequestType = "UnknownRequest";
                    break;
            }
            return response;
        }

        private void CloseHttpContext(HttpListenerContext context, ApiMessage response, HttpStatusCode code = HttpStatusCode.OK)
        {
            context.Response.ContentType = "text/json; charset=UTF-8";
            context.Response.ContentEncoding = Encoding.ASCII;
            var buffer = response.ToJson().ToBuffer();
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = (int) code;
            _eventListener.OnMessage($"Processed client: " +
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
    }
}