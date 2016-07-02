using Newtonsoft.Json;

namespace Next.Accounts_Server.Web_Space
{
    public class JsonParser : IJsonParser
    {
        public T Parse<T>(string source)
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

        public string ToJson<T>(T obj)
        {
            var result = JsonConvert.SerializeObject(obj);
            return result;
        }
    }
}