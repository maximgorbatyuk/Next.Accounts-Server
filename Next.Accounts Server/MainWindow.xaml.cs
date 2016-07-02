using System;
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
using Next.Accounts_Server.Web_Space;

namespace Next.Accounts_Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWebListener
    {

        private WebServer _server;

        public MainWindow()
        {
            InitializeComponent();
            _server = new WebServer(this);
        }

        private void DisplayText(string text)
        {
            LogTextBox.Dispatcher.InvokeAsync(() => LogTextBox.Text += $"{text}\n");
        }


        public async void OnRequestReceived(HttpListenerRequest request)
        {

            if (request.HttpMethod == "GET")
            {
                var parameters = request.QueryString;
                DisplayText(parameters.ToString());
            }

            if (request.HttpMethod == "POST")
            {
                //есть данные от клиента?
                if (!request.HasEntityBody) return;

                //смотрим, что пришло
                using (Stream body = request.InputStream)
                {
                    using (StreamReader reader = new StreamReader(body))
                    {
                        string text = await reader.ReadToEndAsync();
                        text = HttpUtility.UrlDecode(text, Encoding.UTF8);

                        var dic = new Dictionary<string, string>();

                        if (text != null)
                        {
                            var array = text.Split('&');
                            foreach (var item in array)
                            {
                                var pair = item.Split('=');
                                dic.Add(pair[0], pair[1]);
                            }
                        }
                        

                        DisplayText(text);
                        //выводим имя
                        // MessageBox.Show(text);
                    }
                }
            }
            
        }

        public void OnWebSystemMessage(string text)
        {
            DisplayText(text);
        }

        public void OnWebError(Exception ex)
        {
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server.Close();
        }
    }
}
