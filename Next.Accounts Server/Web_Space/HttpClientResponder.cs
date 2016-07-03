using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;

namespace Next.Accounts_Server.Web_Space
{
    public class HttpClientResponder : IHttpListener
    {

        private readonly IEventListener _eventListener;

        public HttpClientResponder(IEventListener eventListener)
        {
            _eventListener = eventListener;
        }


        public void OnRequestReceived(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            string post = null;
            if (request.HttpMethod == "POST")
            {
                var stream = context.Request.InputStream;
                post = new StreamReader(stream).ReadToEnd();
            }


            var responseDic = new Dictionary<string, string>
                    {
                        {"response", context.Response.StatusCode.ToString() },
                        {"raw_url", request.RawUrl },
                        {"method", request.HttpMethod },
                        {"user_agent", request.UserAgent },
                        {"post", post }
                    };

            HttpListenerResponse response = context.Response;
            response.ContentType = "text/json; charset=UTF-8";
            response.ContentEncoding = Encoding.ASCII;

            var buffer = responseDic.ToJson().ToBuffer();
            response.ContentLength64 = buffer.Length;

            Stream output = null;
            try
            {
                //output = response.OutputStream;
                //output.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                _eventListener.OnException(ex);
            }
            finally
            {
                output?.Close();
                response.Close(buffer, false);
            }
        }
    }
}