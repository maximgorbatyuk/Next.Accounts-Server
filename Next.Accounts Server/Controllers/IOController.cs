using System;
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
                StreamReader stream = null;
                try
                {
                    stream = File.OpenText(filename);
                    result = await stream.ReadToEndAsync();
                }
                catch (Exception ex)
                {
                    // ignored
                }
                finally
                {
                    stream?.Close();
                }
            }
            
            return result;
        }

        public static async Task WriteToFileAsync(string filename, string text)
        {
            if (text.Contains("{") && text.Contains("}")) text = text.Replace("\",\"", "\",\n\"");
            StreamWriter stream = null;
            try
            {
                stream = File.CreateText(filename);
                await stream.WriteAsync(text);
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                stream?.Close();
            }
        }

        public static async void AppendToFileAsync(string filename, string text)
        {
            StreamWriter stream = null;
            try
            {
                stream = File.AppendText(filename);
                await stream.WriteAsync(text);
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                stream?.Close();
            }
        }
    }
}