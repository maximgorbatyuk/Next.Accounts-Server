using System.Threading.Tasks;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Application_Space
{
    public interface ISettingsManager
    {
        void SaveSettings(Settings settings);

        Task<Settings> LoadSettings();
    }
}