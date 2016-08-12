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

        private Settings _settings;

        private Dictionary<string, bool> _centersDictionary;

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
            var result = await _requestSender.SendPostDataAsync(request);
            return result;
        }

        public async Task<List<Account>> GetAccountForRequesterAsync(Sender requester, int count = 5)
        {
            var accounts = await _database.GetAccounts(true);
            accounts = accounts.Take(count).ToList();
            foreach (var a in accounts)
            {
                a.Available = false;
                a.ComputerName = $"{requester.Name}({requester.AppType})";
                await _database.UpdateAccountAsync(a);
            }
            accounts.ForEach(a => a.Available = true);
            return accounts;
        }

        public async Task<ApiMessage> CreateResponseForRequester(Sender requester, HttpRequest request, int count = 5)
        {
            var account = await GetAccountForRequesterAsync(requester);
            throw new System.NotImplementedException();
        }
    }
}