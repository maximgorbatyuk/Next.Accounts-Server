using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Server.Extensions
{
    public static class CollectionExtensions
    {
        public static IDictionary<string, IEnumerable<string>> ToDictionary(this NameValueCollection source)
        {
            return source.AllKeys.ToDictionary<string, string, IEnumerable<string>>(key => key, source.GetValues);
        }

        public static NameValueCollection ToNameValueCollection(this IDictionary<string, IEnumerable<string>> source)
        {
            var collection = new NameValueCollection();

            foreach (var key in source.Keys)
            {
                foreach (var value in source[key])
                {
                    collection.Add(key, value);
                }
            }

            return collection;
        }

        public static Models.Account GetRandomAccount(this IEnumerable<Models.Account> source, bool availableOnly = true)
        {
            Models.Account result = null;
            if (source == null) return null;
            var accounts = availableOnly ? source.Where(a => a.Available == true).ToList() : source.ToList();
            result = accounts.Count > 0 ? accounts[Const.GetRandomNumber(accounts.Count)] : null;
            return result;
        }

        
    }
}