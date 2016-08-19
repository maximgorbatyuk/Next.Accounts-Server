using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Database_Namespace.Realize_Classes;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Timers;
using Next.Accounts_Server.Web_Space;
using Next.Accounts_Server.Web_Space.Model;
using Next.Accounts_Server.Web_Space.Realize_Classes;
using Next.Accounts_Server.Windows;

namespace Next.Accounts_Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IEventListener, IDatabaseListener, ISettingsChangedListener, ITimeListener, IResponseListener
    {

        private HttpServer _server;
        private LiteDatabase _database;
        private Settings _settings;
        private bool _firstLaunch = true;
        private WorkTimer _workTimer;
        private Sender _me;

        private IUsedTracker _usedTracker;
        private IServerSpeaker _serverSpeaker;
        private ILogger _logger;
        private IRequestSender _requestSender;
        private ISettingsManager _settingsManager;

        
        ///private TcpServer _tcpServer;

        public MainWindow()
        {
            InitializeComponent();
            InitSettings();
            DisplayIpAddresses();
            _workTimer = new WorkTimer(this);
            var version = $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            VersionLabel.Content = version;
            TestDatabase();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            //AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnUnhandledException(this, null);
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            CurrentDomainOnUnhandledException(sender, null);
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            if (_settings == null || !_settings.CloseOnException) return;
            _logger.LogError("Closed/restarted by unhandled expetion");
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            //Application.Current.Shutdown();
            var me = Process.GetCurrentProcess();
            me.Kill();
        }

        private async void CheckUsedAccounts()
        {
            if (!_firstLaunch) return;
            var used = await _database.GetUsedAccounts();
            if (used?.Count == 0) return;

            _usedTracker.AddAccount(used);
            var text = used?.Aggregate("Server found used accounts in database. Here is a list:\r\n", (current, a) => current + $"{a}\r\n");

            if (text != null) DisplayText(text);
            _firstLaunch = false;
        }

        private async void InitSettings(Settings s = null)
        {
            _settingsManager = new DefaultSettingsManager();
            if (s != null) _settings = s;
            else _settings = await _settingsManager.LoadSettings();

            _me             = Const.GetSender(name: _settings.CenterName, client: false);
            _logger         = new DefaultLogger();
            _requestSender  = new WebClientController(listener: this, responseListener: this);
            _database       = new LiteDatabase(listener: this, dbListener: this, dbName: _settings.DatabaseName);
            _serverSpeaker  = new DefaultServerSpeaker(_settings, _database, _requestSender, this);
            _usedTracker    = new DefaultUsedTracker( _settings.UsedMinuteLimit);
            var getResponder = new GetResponder(_database, _me, _settings);

            var clientProcessor = new HttpClientResponder(_me, _settings)
            {
                Database = _database,
                EventListener = this,
                UsedTracker = _usedTracker,
                ServerSpeaker = _serverSpeaker,
                GetResponder = getResponder,
                SettingsChangedListener = this
            };

            _server?.Close();
            var url = $"http://+:{_settings.Port}/";
            _server = new HttpServer(clientProcessor, this, url);
            CheckUsedAccounts();
            StartListenButton_OnClick(this, null);
        }

        private void DisplayIpAddresses()
        {
            var addresses = Const.GetAddresses().ToList();
            var main = addresses.FirstOrDefault(a => a.ToString().Contains("192.168.1.") || a.ToString().Contains("10.5.8."));
            var text = addresses.Aggregate("Available IP addresses:\r\n", (current, ip) => current + $"{ip.ToString()}\r\n");
            
            if (main != null) AddressLabel.Content = main;
            DisplayText(text);
        }

        private void DisplayText(string text)
        {
            var time = DateTime.Now;
            LogTextBox.Dispatcher.InvokeAsync(() => LogTextBox.Text += $"[{time}] {text}\n");
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server?.Close();
            _workTimer?.Stop();
        }

        private async void StartListenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_server.GetListenState())
            {
                await StartListenButton.Dispatcher.InvokeAsync(() => StartListenButton.Header = "Start listenning");
                _server.Close();
                await ServerMenuItem.Dispatcher.InvokeAsync(() =>
                    ServerMenuItem.Background = new SolidColorBrush(Color.FromArgb(255, 227, 158, 158)));
            }
            else
            {
                while (!_server.GetListenState())
                {
                    await Task.Delay(100);
                    _server.Start();
                }
                    await StartListenButton.Dispatcher.InvokeAsync(() => StartListenButton.Header = "Stop listenning");
                
                await ServerMenuItem.Dispatcher.InvokeAsync(() => 
                    ServerMenuItem.Background = new SolidColorBrush(Color.FromArgb(255, 158, 227, 174)));
                
            }
        }

        public void OnException(Exception ex)
        {
            var text = $"Exception catched:\r\nMessage: {ex.Message}";
            DisplayText(text);
            _logger.LogError(ex.Message);
        }


        public void OnEvent(string message, object sender = null)
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
            Dispatcher.InvokeAsync(() =>
            {
                _settingsManager.SaveSettings(settings);
                InitSettings(settings);
            } ) ;

            //throw new NotImplementedException();
        }

        private async void TestDatabase()
        {
            
            //while (_serverSpeaker == null) { await Task.Delay(100); }
            //var resp = await _serverSpeaker.CreateResponseForRequester( _me, null);
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
            if (LogTextBox.Text.Length == 0) return;
            LogTextBox.CaretIndex = LogTextBox.Text.Length - 1;
        }

        private void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/maximgorbatyuk/Next.Accounts-Server";
            Process.Start(url);
        }

        public async void OnServerResponse(string responseString)
        {
            var api = responseString.ParseJson<ApiMessage>();
            if (api == null) return;
            var sender = api.JsonSender.ParseJson<Sender>();
            if (sender == null)
            {
                DisplayText($"Got invalid api: {api}");
                return;
            }
            if (sender.AppType != Const.ServerAppType) return;
            string display = null;
            if (api.Code == 404 || api.JsonObject == null)
            {
                display = $"Server {sender} have no free accounts";
            }
            else
            {
                var accounts = api.JsonObject.ParseJson<List<Account>>();
                await _database.AddAccountAsync(accounts);
                display = $"Have got a {accounts.Count} of accounts from {sender}";
            }
            DisplayText(display);

            
        }

        public void OnConnectionError(Exception ex)
        {
            DisplayText($"Could not connect to other servers. Exception: {ex.Message}");
        }

        private void ClearTextBoxMenu_OnClick(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";
        }

        private void ExMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
