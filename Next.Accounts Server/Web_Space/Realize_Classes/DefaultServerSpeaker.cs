using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space.Realize_Classes
{
    public class DefaultServerSpeaker : IServerSpeaker
    {
        private readonly IDatabase _database;

        private readonly IRequestSender _requestSender;

        private readonly IEventListener _listener;

        private readonly Settings _settings;

        private readonly Dictionary<string, bool> _centersDictionary;


        public DefaultServerSpeaker(Settings settings, IDatabase database, IRequestSender requestSender, IEventListener listener)
        {
            _settings = settings;
            _database = database;
            _requestSender = requestSender;
            _listener = listener;
            _centersDictionary = new Dictionary<string, bool>();
            foreach (var address in _settings.AddressesList)
            {
                _centersDictionary.Add(address, false);
            }
        }

        private string ResetAndGetFirst()
        {
            foreach (var key in _centersDictionary.Keys)
            {
                _centersDictionary[key] = false;
            }
            return _centersDictionary.Keys.First();
        }


        public async Task<bool> AskAccounts(Sender me)
        {
            if (!_settings.AskAccounts)
            {
                _listener.OnEvent("Request for extra accounts has NOT been sent because of settings permissions");
                return false;
            }
            string address = null;
            foreach (var key in _centersDictionary.Keys.ToList())
            {
                if (_centersDictionary[key] == true) continue;
                _centersDictionary[key] = true;
                address = key;
                break;
            }
            address = address ?? ResetAndGetFirst();
            if (string.IsNullOrWhiteSpace(address)) return false;
            var request = new ApiMessage
            {
                Code = 200,
                JsonObject = null,
                JsonSender = me.ToJson(),
                RequestType = Const.RequestTypeGet,
                StringMessage = "Give me accounts, please"
            };
            var result = await _requestSender.SendPostDataAsync(request, address);
            var message = result ? $"Have sent a request for account to server: {address}" : $"Tried to connect to server {address}, but no connection";
            _listener.OnEvent(message);
            return result;
        }

        public async Task<List<Account>> GetAccountForRequesterAsync(Sender requester, int count)
        {
            var accounts = await _database.GetAccounts(true);
            if (accounts == null) return null;
            accounts = accounts.Take(count).ToList();
            foreach (var a in accounts)
            {
                await _database.RemoveAccountAsync(a);
                //a.Available = false;
                //a.ComputerName = $"{requester.Name}({requester.AppType})";
                //await _database.UpdateAccountAsync(a);
            }
            //accounts.ForEach(a => a.Available = true);
            return accounts;
        }

        public async Task<ApiMessage> CreateResponseForRequester(Sender requester, Sender me, HttpRequest request, int count = 5)
        {
            var accounts = await GetAccountForRequesterAsync(requester, count);
            var response = new ApiMessage
            {
                Code = 200,
                JsonObject = null,
                RequestType = Const.RequestTypeGet,
                StringMessage = request.HttpMethod == "POST" ? request.PostData : request.RawUrl,
                JsonSender = me.ToJson()
            };
            string message = null;
            if (accounts == null || accounts.Count == 0 || !_settings.GiveAccounts)
            {
                response.Code = 404;
                message = $"Denied request for accounts from {requester}";
            }
            else
            {
                response.JsonObject = accounts.ToJson();
                message = $"have prepared {accounts.Count} for server {requester} and sent it";
                response.RequestType = Const.RequestTypeGet;
            }
            _listener.OnEvent(message);
            return response;
        }
    }
}