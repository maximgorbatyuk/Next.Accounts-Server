using System.Collections.Generic;

namespace Next.Accounts_Client.Application_Space
{
    public class ClientSettings
    {
        public string OkayMessage { get; set; } = "Данные аккаунта приняты. Запускаю Steam";

        public string CancelMessage { get; set; } = "К сожалению, Вы не можете сейчас воспользоваться аккаунтом";

        public string BadConnectionMessage { get; set; } = "Соединение с сервером отсутствует. Обратитесь к оператору";

        public string NoAvailableAccountsMessage { get; set; } = "Отсутствуют свободные аккаунты. Обратитесь к оператору";

        public string LimitMessage { get; set; } = "Достигнут предел выделенных аккаунтов. Обратитесь к оператору";

        public string ReleasedMessage { get; set; } = "Аккунт освобожден";

        public string IpAddress { get; set; } = "http://accounts.next.kz/rest/distribution.php";

        public string ProcessName { get; set; } = "steam";

        public string SteamDirectory { get; set; } = "C:\\Program Files (x86)\\Steam\\steam.exe";

        public string CenterName { get; set; } = "test";

        public List<string> VacBanGames { get; set; } = new List<string> ();
    }
}