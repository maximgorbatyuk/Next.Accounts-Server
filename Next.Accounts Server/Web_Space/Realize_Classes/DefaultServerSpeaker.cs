using System;
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
                try
                {
                    _centersDictionary.Add(address, false);
                }
                catch (Exception ex)
                {
                    // ignored
                }
            }
        }

        private string ResetAndGetFirst()
        {
            foreach (var key in _centersDictionary.Keys.ToList())
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

        public async Task<ApiMessage> CreateResponseForRequester(ApiRequests type, Sender requester, Sender me, ApiMessage request, int count = 5)
        {
            string message = null;
            ApiMessage response = new ApiMessage
            {
                Code = 200,
                JsonObject = null,
                RequestType = null,
                StringMessage = request?.ToJson(),
                JsonSender = me.ToJson()
            }; ;

            if (request == null) return null;

            switch (type)
            {
                case ApiRequests.GetAccount:
                    if (_settings.GiveAccounts)
                    {
                        var accountsToSend = await GetAccountForRequesterAsync(requester, count);
                        

                        if (accountsToSend == null || accountsToSend.Count == 0)
                        {
                            response.Code = 404;
                            response.RequestType = Const.RequestTypeGet;
                            message = $"Denied request for accounts from {requester} because of null/zero count of available accounts";
                        }
                        else
                        {
                            response.JsonObject = accountsToSend.ToJson();
                            message = $"have prepared {accountsToSend.Count} for server {requester} and sent it";
                            response.RequestType = Const.RequestTypeGet;
                        }
                    }
                    else
                    {
                        message = $"Denied request for accounts from {requester} because of settings permissions";
                    }
                    break;
                case ApiRequests.ReleaseAccount:
                    var accountsToRelease = request.JsonObject.ParseJson<List<Account>>();
                    if (accountsToRelease != null)
                    {
                        await _database.AddAccountAsync(accountsToRelease);
                        response.RequestType = Const.RequestTypeRelease;
                        response.Code = 200;
                        message =
                            $"Have received {accountsToRelease.Count} of accounts from {requester} as Release operation";
                    }
                    else
                    {
                        message =
                            $"Have received a Release request from {requester}, but a list of accounts is null";
                        response = null;
                    }
                    break;
                case ApiRequests.UsingAccount:
                case ApiRequests.Unknown:
                case ApiRequests.None:
                default:
                    response = null;
                    break;
            }
            
            _listener.OnEvent(message);
            return response;
        }
    }
}