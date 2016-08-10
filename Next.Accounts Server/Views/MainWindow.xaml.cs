using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Timers;
using Next.Accounts_Server.Web_Space;
using Next.Accounts_Server.Windows;

namespace Next.Accounts_Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IEventListener, IDatabaseListener, ISettingsChangedListener, ITimeListener
    {

        private HttpServer _server;
        private LiteDatabase _database;
        private Settings _settings;
        private IUsedTracker _usedTracker;
        private bool _firstLaunch = true;
        private WorkTimer _workTimer;
        private ILogger _logger;
        ///private TcpServer _tcpServer;

        public MainWindow()
        {
            InitializeComponent();
            InitSettings();
            DisplayIpAddresses();
            _workTimer = new WorkTimer(this);
        }

        private async void CheckUsedAccounts()
        {
            if (!_firstLaunch) return;
            var used = await _database.GetAccounts(false);
            if (used?.Count == 0) return;

            _usedTracker.AddAccount(used);
            var text = "Server found used accounts in database. Here is a list:\r\n";

            foreach (var a in used)
            {
                text += $"{a}\r\n";
            }
            DisplayText(text);
            _firstLaunch = false;
        }

        private async void InitSettings(Settings s = null)
        {
            if (s != null) _settings = s;
            else _settings = await LoadOrGetDefault();

            _logger = new DefaultLogger();
            _database = new LiteDatabase(this, this, _settings.DatabaseName);

            var me = Const.GetSender(client: false);
            _usedTracker = new DefaultUsedTracker( 2 /*_settings.UsedMinuteLimit*/);
            var clientProcessor = new HttpClientResponder(me, _settings)
            {
                Database = _database,
                EventListener = this,
                UsedTracker = _usedTracker
            };
            _server?.Close();
            _server = new HttpServer(clientProcessor, this);
            CheckUsedAccounts();
            StartListenButton_OnClick(this, null);
        }

        private async Task<Settings> LoadOrGetDefault()
        {
            var settingsText = await IoController.ReadFileAsync(Const.SettingsFilename);
            Settings settings = null;
            if (settingsText == null)
            {
                settings = new Settings();
                IoController.WriteToFileAsync(Const.SettingsFilename, _settings.ToJson());
            }
            else
            {
                settings = settingsText.ParseJson<Settings>();
            }
            return settings;
        }

        private void DisplayIpAddresses()
        {
            var addresses = Const.GetAddresses().ToList();
            var main = addresses.FirstOrDefault(a => a.ToString().Contains("192.168.1."));
            var text = addresses.Aggregate("Available IP addresses:\r\n", (current, ip) => current + $"{ip.ToString()}\r\n");
            
            if (main != null) AddressLabel.Content = main;
            DisplayText(text);
        }

        private void DisplayText(string text)
        {
            LogTextBox.Dispatcher.InvokeAsync(() => LogTextBox.Text += $"{text}\n");
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server?.Close();
            _workTimer?.Stop();
        }

        private void StartListenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_server.GetListenState())
            {
                StartListenButton.Header = "Start listenning";
                _server.Close();
            }
            else
            {
                StartListenButton.Header = "Stop listenning";
                _server.Start();
            }
        }

        public void OnException(Exception ex)
        {
            var text = $"Exception catched:\nStack: {ex.StackTrace}\nMessage: {ex.Message}";
            DisplayText(text);
            _logger.LogError(ex.Message);
        }

        public void OnMessage(string message)
        {
            DisplayText(message);
            _logger.Log(message);
        }

        public void UpdateAccountCount(int count, int available)
        {
            var text = $"Accounts in DB: {count}, available: {available}";
            CountLabel.Dispatcher.InvokeAsync(() => CountLabel.Content = text);
        }

        public void OnSettingsChanged(Settings settings)
        {
            InitSettings(settings);
            //throw new NotImplementedException();
        }

        private async void TestDatabase()
        {
            if (_database == null) return;
            var accounts = await _database.GetAccounts();
            DisplayText($"Got {accounts.Count} of accounts");
            foreach (var a in accounts)
            {
                a.Available = false;
            }
            var count = await _database.UpdateAccountAsync(accounts);
            DisplayText($"Updated {count} rows");
        }

        private void OpenSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            SettingsWindows settingsWindows = new SettingsWindows(_database, _settings, this);
            settingsWindows.ShowDialog();
        }

        private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var currentFolder = Environment.CurrentDirectory;
            Process.Start(currentFolder);
        }

        public void UpdateTime(TimeSpan difference)
        {
            var hours = difference.Hours < 10 ? $"0{difference.Hours}" : difference.Hours.ToString();
            var min = difference.Minutes < 10 ? $"0{difference.Minutes}" : difference.Minutes.ToString();
            var sec = difference.Seconds < 10 ? $"0{difference.Seconds}" : difference.Seconds.ToString();
            var time = $"Work time: {hours}:{min}:{sec}";
            TimeDisplayer.Dispatcher.InvokeAsync(() => TimeDisplayer.Content = time);
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogTextBox.ScrollToEnd();
            LogTextBox.CaretIndex = LogTextBox.Text.Length - 1;
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/maximgorbatyuk/Next.Accounts-Server");
            //throw new NotImplementedException();
        }
    }
}
