using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Database_Namespace.Realize_Classes;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Windows
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindows.xaml
    /// </summary>
    public partial class SettingsWindows : Window, IEventListener
    {
        private ISettingsChangedListener _listener;
        private readonly IDatabase _database;
        private PostgresDatabase _postgres;
        private Settings _source;
        private string _backupFilename = "accounts.js";
        private Account _selectedAccount = null;
        private bool _isLocalStorageSelected = false;

        public SettingsWindows(IDatabase database, Settings source, ISettingsChangedListener listener)
        {
            InitializeComponent();
            _source = source;
            _listener = listener;
            _database = database;
            LoadSettingsToComponents(source);
            _postgres = new PostgresDatabase(_source, this);
        }

        private void LoadSettingsToComponents(Settings source)
        {
            ConnStringTextBox.Text = source.PostgresConnectionString;
            CenterNameTextBox.Text = source.CenterName;
            AskAccountsCheck.IsChecked = source.AskAccounts;
            AddressesTextBox.Text = source.AddressesList != null
                ? source.AddressesList.Aggregate("", (current, ip) => current + $"{ip}\r\n") : "";
            GiveAccountsCheck.IsChecked = source.GiveAccounts;
            IssueLimitCheck.IsChecked = source.SetIssueLimit;
            IssueLimitTextBox.Text = source.IssueLimitValue.ToString();
            ExpiredTextBox.Text = source.UsedMinuteLimit.ToString();
            CloseOnExCheck.IsChecked = source.CloseOnException;
            MinimalTextBox.Text = $"{source.MinimalAccountLimit}";
        }

        private Settings GetSettingsFromComponents()
        {
            var addressesText = AddressesTextBox.Text.Replace("\r", "");
            var addresses = addressesText.Split('\n').ToList();
            addresses = addresses.Where(a => !string.IsNullOrWhiteSpace(a.ToString())).ToList();

            var issueLimit = !string.IsNullOrWhiteSpace(IssueLimitTextBox.Text) ? int.Parse(IssueLimitTextBox.Text) : 10;
            var expired = !string.IsNullOrWhiteSpace(ExpiredTextBox.Text) ? int.Parse(ExpiredTextBox.Text) : 5;
            var minimal = !string.IsNullOrWhiteSpace(MinimalTextBox.Text) ? int.Parse(MinimalTextBox.Text) : 5;
            var settings = new Settings
            {
                PostgresConnectionString = ConnStringTextBox.Text,
                CenterName = CenterNameTextBox.Text,
                AskAccounts = AskAccountsCheck.IsChecked != null && AskAccountsCheck.IsChecked.Value,
                AddressesList = addresses,
                GiveAccounts = GiveAccountsCheck.IsChecked != null && GiveAccountsCheck.IsChecked.Value,
                SetIssueLimit = IssueLimitCheck.IsChecked != null && IssueLimitCheck.IsChecked.Value,
                IssueLimitValue = issueLimit,
                UsedMinuteLimit = expired,
                DefaultSettings = false,
                CloseOnException = CloseOnExCheck.IsChecked != null && CloseOnExCheck.IsChecked.Value,
                MinimalAccountLimit = minimal
            };
            return settings;
        }

        private async void Test()
        {
            var check = await _postgres.CheckConnection();
            var centerName = "test_club";
            var accounts = await _postgres.GetAccounts($"WHERE club='{centerName}'");
            if (accounts == null) return;

            var restored = await _database.RestoreAccounts(accounts);
            OnEvent($"Restored {restored} of accounts");
        }

        public void OnException(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        public void OnEvent(string message, object sender = null)
        {
            MessageBox.Show(message);
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _source = GetSettingsFromComponents();
            _listener.OnSettingsChanged(_source);
            await IoController.WriteToFileAsync(Const.SettingsFilename, _source.ToJson());
            CancelButton_Click(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConfirmation()) return;
            var accounts = await _database.GetAccounts();
            await IoController.WriteToFileAsync(_backupFilename, accounts.ToJson());
            DisplayInfo($"{accounts.Count} of accounts have been backuped");
        }

        private async void DisplayInfo(string message, bool error = false)
        {
            InfoLabel.Content = message;
            InfoLabel.Background = new SolidColorBrush( 
                error ? 
                Color.FromArgb(255, 255, 164, 164) : 
                Color.FromArgb(255, 152, 219, 182));
            await Task.Delay(2000);
            InfoLabel.Content = "";
            InfoLabel.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
        }

        private bool CheckConfirmation()
        {
            var check = ConfirmCheck.IsChecked != null && ConfirmCheck.IsChecked.Value;
            if (check) return check;
            DisplayInfo("Confirm your actions");
            return false;
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConfirmation()) return;
            var accountsString = await IoController.ReadFileAsync(_backupFilename);
            if (accountsString == null)
            {
                DisplayInfo("Backup file does not exists", true);
                return;
            }
            var accounts = accountsString.ParseJson<List<Account>>();
            if (accounts == null || accounts?.Count == 0)
            {
                DisplayInfo("Backup file is null or empty", true);
                return;
            }
            var count = await _database.AddAccountAsync(accounts);
            DisplayInfo($"{count} of accounts have been restored from backup");
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConfirmation()) return;
            var count = await _database.DeleteAccountsTable();
            var accounts = await _postgres.GetAccounts($"WHERE club='{_source.CenterName}'");
            if (accounts == null) return;

            var restored = await _database.RestoreAccounts(accounts);
            DisplayInfo($"Restored {restored} of accounts");

        }

        private async void DeleteLocalButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConfirmation()) return;
            var count = await _database.DeleteAccountsTable();
            DisplayInfo($"Local database of accounts has been cleared up");
        }

        private async void CheckConnButton_Click(object sender, RoutedEventArgs e)
        {
            var check = await _postgres.CheckConnection();
            var message = check ? "Connected to postgres" : "No connection to postgres";
            DisplayInfo(message, !check);
        }

        private void LoadListBox(IList<Account> source)
        {
            AccountListBox.Items.Clear();
            if (source == null) source = new List<Account>();
            foreach (var a in source)
            {
                AccountListBox.Items.Add(a);
            }
            DisplayInfo($"{source.Count} of items have been loaded");
        }

        private async void LiteLoadButton_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await _database.GetAccounts();
            LoadListBox(accounts);
            CenterTextBox.IsEnabled = false;
            _isLocalStorageSelected = true;
        }

        private async void PostgresLoadButton_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await _postgres.GetAccounts();
            LoadListBox(accounts);
            CenterTextBox.IsEnabled = true;
            _isLocalStorageSelected = false;
        }

        private void LoadAccountToComponents(Account source)
        {
            _selectedAccount = source;
            LoginTextBox.Text = source.Login;
            PasswordTextBox.Text = source.Password;
            AvailableCheck.IsChecked = source.Available;
            ComputerTextBox.Text = source.ComputerName;
            CenterTextBox.Text = source.CenterOwner;
            CheckBox_VacBanned.IsChecked = source.VacBanned;
        }

        private void AccountListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = AccountListBox.SelectedItem;
            if (item != null) LoadAccountToComponents((Account) item);
        }

        private async void ReturneAccountsButton_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await _database.GetAccounts(predicate: $"WHERE Owner!='{_source.CenterName}'");
            DisplayInfo($"Here is a {accounts?.Count} of not mine accounts ");
        }

        private async void Button_SaveVacStatus_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_isLocalStorageSelected || _selectedAccount == null)
            {
                DisplayInfo($"Remote DB has been loaded", true);
                return;
            }

            var selected = _selectedAccount;
            var status = CheckBox_VacBanned.IsChecked != null && CheckBox_VacBanned.IsChecked.Value;
            selected.VacBanned = status;
            var result = await _database.UpdateAccountAsync(selected);
            DisplayInfo($"VAC status has been updated ({result})");
        }
    }
}
