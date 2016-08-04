using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Web_Space
{
    public class HttpClientProcessor : IHttpProcessor
    {

        private readonly IEventListener _eventListener;

        public HttpClientProcessor(IEventListener eventListener)
        {
            _eventListener = eventListener;
        }


        public void OnRequestReceived(HttpListenerContext context)
        {
            var request = new Request
            {
                HttpMethod = context.Request.HttpMethod,
                Headers = context.Request.Headers.ToDictionary(),
                InputStream = context.Request.InputStream,
                RawUrl = context.Request.RawUrl
            };

            //ReturnWebError(context, "Fuck you");
            //return;

            if (request.HttpMethod == "POST")
            {
                request.PostData = new StreamReader(request.InputStream).ReadToEnd();
            }

            var account = new Account { Id = 1488, Login = "maxim", Password = "password" };

            var response = new Response
            {
                Code = 200,
                Request = request.HttpMethod == "POST" ? request.PostData : request.RawUrl,
                Result = account.ToJson()
            };

            context.Response.ContentType = "text/json; charset=UTF-8";
            context.Response.ContentEncoding = Encoding.ASCII;

            var buffer = response.ToJson().ToBuffer();
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = (int) HttpStatusCode.OK;
            _eventListener.OnMessage($"Processed client: {context.Request.LocalEndPoint?.Address}");
            context.Response.Close(buffer, false);
            
        }

        public void ReturnWebError(HttpListenerContext context, string message, int code = 404)
        {
            //
            context.Response.StatusCode = code;
            context.Response.Close(message.ToBuffer(), false);
        }
    }
}