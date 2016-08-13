using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
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

        private readonly Settings _settings;

        private readonly Dictionary<string, bool> _centersDictionary;


        public DefaultServerSpeaker(Settings settings, IDatabase database, IRequestSender requestSender)
        {
            _settings = settings;
            _database = database;
            _requestSender = requestSender;
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
            string address = null;
            foreach (var key in _centersDictionary.Keys)
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
            if (accounts?.Count > 0)
            {
                response.JsonObject = accounts.ToJson();
                response.RequestType = Const.RequestTypeGet;
            }
            else
            {
                response.Code = 404;
            }
            return response;
        }
    }
}