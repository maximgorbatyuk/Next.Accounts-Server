namespace Next.Accounts_Client.App_data
{
    public class Settings
    {
        public string OkayMessage { get; set; } = "Данные аккаунта приняты. Запускаю Steam";

        public string CancelMessage { get; set; } = "К сожалению, Вы не можете сейчас воспользоваться аккаунтом";

        public string BadConnectionMessage { get; set; } = "Соединение с сервером отсутствует. Обратитесь к оператору";

        public string NoAvailableAccountsMessage { get; set; } = "Отсутствуют свободные аккаунты. Обратитесь к оператору";

        public string LimitMessage { get; set; } = "Достигнут предел выделенных аккаунтов. Обратитесь к оператору";

        public string ReleasedMessage { get; set; } = "Аккунт освобожден";

        public string IpAddress { get; set; } = "127.0.0.1";

        public string ProcessName { get; set; } = "steam";

        public string SteamDirectory { get; set; } = "C:\\Program Files (x86)\\Steam\\steam.exe";
    }
}