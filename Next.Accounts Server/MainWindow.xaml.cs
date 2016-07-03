﻿using System;
using System.Collections.Generic;
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
using Next.Accounts_Server.Web_Space;

namespace Next.Accounts_Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IEventListener
    {

        private HttpServer _server;
        private TcpServer _tcpServer;

        public MainWindow()
        {
            InitializeComponent();
            var httpResponder = new HttpClientResponder(this);
            _server = new HttpServer(httpResponder, this);

            //var tcpResponder = new TcpClientResponder(this);
            //_tcpServer = new TcpServer(tcpResponder, this);
        }

        private void DisplayText(string text)
        {
            LogTextBox.Dispatcher.InvokeAsync(() => LogTextBox.Text += $"{text}\n");
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server?.Close();
            _tcpServer?.Stop();
        }

        private void StartListenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_server.GetListenState())
            {
                StartListenButton.Header = "Start listenning";
                _server.Close();
                //_tcpServer.Stop();
            }
            else
            {
                StartListenButton.Header = "Stop listenning";
                _server.Start();
               // _tcpServer.Start();
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
    }
}
