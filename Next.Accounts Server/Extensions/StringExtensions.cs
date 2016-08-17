using System;
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

        public static int ToInt(this bool value)
        {
            var result = 0;
            if (value) result = 1;
            return result;
        }

        public static int ParseInt(this string source, int defaultValue = -1)
        {
            int result = -1;
            if (source == null) return defaultValue;
            try
            {
                var parse = int.Parse(source);
                result = parse;
            }
            catch (Exception ex)
            {
                //ignored
                result = defaultValue;
            }
            return result;
        }
    }
}