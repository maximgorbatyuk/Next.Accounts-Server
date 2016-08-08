﻿using System;
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
using Next.Accounts_Server.Web_Space;
using Next.Accounts_Server.Windows;

namespace Next.Accounts_Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IEventListener, IDatabaseListener, ISettingsChangedListener
    {

        private HttpServer _server;
        private LiteDatabase _database;
        private Settings _settings;
        ///private TcpServer _tcpServer;

        public MainWindow()
        {
            InitializeComponent();
            InitSettings();
            DisplayIpAddresses();
            
        }

        private async void InitSettings(Settings s = null)
        {
            if (s != null) _settings = s;
            else
            {
                //var filename = $"{Environment.CurrentDirectory}\\App_data\\{Const.SettingsFilename}";
                var settingsText = await IoController.ReadFileAsync(Const.SettingsFilename);
                if (settingsText == null)
                {
                    _settings = new Settings();
                    IoController.WriteToFileAsync(Const.SettingsFilename, _settings.ToJson());
                }
                else
                {
                    _settings = settingsText.ParseJson<Settings>();
                }
                
            }
            _database = new LiteDatabase(this, this, _settings.DatabaseName);
            var me = new Sender()
            {
                AppType = Const.ServerAppType,
                AppVersion = "1",
                IpAddress = Const.GetAddresses().Where(a => a.ToString().Contains("192.168.1")).ToString(),
                Name = _settings.CenterName
            };
            var clientProcessor = new HttpClientResponder(this, _database, me);
            _server             = new HttpServer(clientProcessor, this);
            
        }

        private void DisplayIpAddresses()
        {
            var text = "Сервер доступен по следующим адресам:\r\n";
            var addresses = Const.GetAddresses();
            foreach (var ip in addresses)
            {
                text += $"{ip.ToString()}\r\n";
            }
            DisplayText(text);
        }

        private void DisplayText(string text)
        {
            LogTextBox.Dispatcher.InvokeAsync(() => LogTextBox.Text += $"{text}\n");
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server?.Close();
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
        }

        public void OnMessage(string message)
        {
            DisplayText(message);
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
            var accounts = await _database.GetListOfAccountsAsync();
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

        private void TestFunctionButton_OnClick(object sender, RoutedEventArgs e)
        {
            TestDatabase();
        }

        private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var currentFolder = Environment.CurrentDirectory;
            Process.Start(currentFolder);
            
        }
    }
}