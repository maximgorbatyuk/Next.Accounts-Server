using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Application_Space
{
    public interface ISettingsChangedListener
    {
        void OnSettingsChanged(Settings settings);
    }
}