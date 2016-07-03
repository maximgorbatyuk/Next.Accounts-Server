using System.Collections.Generic;
using System.Text;

namespace Next.Accounts_Server.Extensions
{
    public static class StringExtensions
    {
        public static byte[] ToBuffer(this string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
    }
}