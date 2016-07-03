using Newtonsoft.Json;

namespace Next.Accounts_Server.Extensions
{
    public static class JsonExtensions
    {
        public static T Parse<T>(this string source)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(source);
                return result;
            }
            catch (JsonException ex)
            {
                // _displayer?.OnEvent(ex.Message);
                // _outputController?.LogError(ex.Message);
            }
            return default(T);
        }

        public static string ToJson<T>(this T obj)
        {
            var result = JsonConvert.SerializeObject(obj);
            return result;
        }
    }
}