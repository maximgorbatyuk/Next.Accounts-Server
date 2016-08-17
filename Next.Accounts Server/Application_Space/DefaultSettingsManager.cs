using System;
using System.Threading.Tasks;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Application_Space
{
    public class DefaultSettingsManager : ISettingsManager
    {
        public async void SaveSettings(Settings settings)
        {
            await IoController.WriteToFileAsync(Const.SettingsFilename, settings.ToJson());
        }

        public async Task<Settings> LoadSettings()
        {
            var settingsText = await IoController.ReadFileAsync(Const.SettingsFilename);
            Settings settings = null;
            if (settingsText == null || settingsText == "null")
            {
                settings = new Settings();
                SaveSettings(settings);
            }
            else
            {
                settings = settingsText.ParseJson<Settings>();
            }
            return settings;
        }
    }
}