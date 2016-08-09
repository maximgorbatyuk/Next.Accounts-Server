using System.IO;
using System.Threading.Tasks;

namespace Next.Accounts_Server.Controllers
{
    public static class IoController
    {
        public static async Task<string> ReadFileAsync(string filename)
        {
            string result = null;
            if (File.Exists(filename))
            {
                using (var stream = File.OpenText(filename))
                {
                    result = await stream.ReadToEndAsync();
                }
            }
            
            return result;
        }

        public static async Task WriteToFileAsync(string filename, string text)
        {
            using (var stream = File.CreateText(filename))
            {
                await stream.WriteAsync(text);
            }
        }

        public static async void AppendToFileAsync(string filename, string text)
        {
            using (var stream = File.AppendText(filename))
            {
                await stream.WriteAsync(text);
            }
        }
    }
}