using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Server.Web_Space
{
    public interface IServerSpeaker
    {
        Task<bool> AskAccounts(Sender me);

        Task<List<Account>> GetAccountForRequesterAsync(Sender requester, int count = 5);

        Task<ApiMessage> CreateResponseForRequester(Sender requester, HttpRequest request, int count = 5);
    }
}