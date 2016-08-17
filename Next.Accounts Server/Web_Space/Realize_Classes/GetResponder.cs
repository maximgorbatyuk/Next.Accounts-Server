using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Web_Space.Realize_Classes
{
    public class GetResponder : IGetResponder
    {
        private readonly IDatabase _database;

        private readonly Sender _me;

        private readonly Settings _settings;

        public GetResponder(IDatabase database, Sender me, Settings settings)
        {
            _database = database;
            _me = me;
            _settings = settings;
        }

        public async Task<string> GetHtmlPage(HttpListenerContext context, string raw = "/", string message = null, bool error = false)
        {
            var result = await GetDefaultHtml(context, raw, message, error) ??
                           "<h1>Infopage.html does not exists</h1><br><br><br>" +
                           "<h2>Load it from github or ask a <a href='https://new.vk.com/maximgorbatyuk'>developer</a> for it</h2>";
            return result;
        }

        private async Task<string> GetDefaultHtml(HttpListenerContext context, string raw = "/", string message = null, bool error = false)
        {

            
            string html = null;
            switch (raw)
            {
                case "/":
                    html = await IoController.ReadFileAsync(Const.IndexPageFilename);
                    var accounts = await _database.GetAccounts();
                    if (html == null) return null;
                    var meText = $"<tr><td>Version of the ap</td><td>{_me.AppVersion}</td></tr>" +
                                 $"<tr><td>My name (displayed in Sender.Name field)</td><td>{_me.Name}</td></tr>" +
                                 $"<tr><td>Local IP</td><td>{_me.IpAddress}</td></tr>";
                    //html = html.Replace("#CenterName", _settings.CenterName).Replace("#me", meText);
                    string accountList = "No accounts in local storage (Null data)";
                    string footer = "No data";
                    if (accounts != null)
                    {
                        accountList = "No accounts in local storage";
                        if (accounts.Count > 0)
                        {
                            accountList = accounts.Aggregate("",
                                (current, a) => current + $"<li class=\"list-group-item\">{a}</li>");
                            footer = $"Amount of accounts is {accounts.Count}, available of them - {accounts.Count(a => a.Available == true)}";
                        }
                    }
                
                    html = html
                        .Replace("#accountlist", accountList)
                        .Replace("#CenterName", _settings.CenterName)
                        .Replace("#me", meText)
                        .Replace("#footer", footer);
                    break;

                case "/settings":
                    html = await IoController.ReadFileAsync(Const.SettingsPageFilename);
                    html = html.Replace("#AddressesList", _settings.AddressesList.Aggregate("", (current, a) => current + $"{a}\n"));
                    html = html.Replace("#AskAccounts", _settings.AskAccounts ? "checked" : " ");
                    html = html.Replace("#GiveAccounts", _settings.GiveAccounts ? "checked" : " ");
                    html = html.Replace("#SetIssueLimit", _settings.SetIssueLimit ? "checked" : " ");
                    html = html.Replace("#IssueLimitValue", $"{_settings.IssueLimitValue}");
                    html = html.Replace("#UsedMinuteLimit", $"{_settings.UsedMinuteLimit}");
                    break;
            }
            if (html == null) return null;
            var request = context.Request;
            var senderText = $"Request data:<br>" +
                             $"HttpMethod: {request.HttpMethod}<br>" +
                             $"End point: {request.RemoteEndPoint?.Address.ToString()}:{request.RemoteEndPoint?.Port}<br>" +
                             $"User agent: {request.UserAgent}<br>" +
                             $"Raw url: {request.RawUrl}";
            html = html.Replace("#sender", senderText);
            var type = !error ? "alert alert-success" : "alert alert-danger";
            var head = !error ? "Success" : "Error";
            html = html.Replace("#alert", message != null ? $"<div class=\"{type}\">" +
                            "<a href=\"#\" class=\"close\" data-dismiss=\"alert\" aria-label=\"close\">&times;</a>" +
                            $"<strong>{head}!</strong> {message}" +
                            "</div>" : "");
            return html;

        }
    }
}