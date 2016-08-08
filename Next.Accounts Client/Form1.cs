using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Next.Accounts_Client.Web_Space;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Client
{
    public partial class Form1 : Form, Accounts_Server.Application_Space.IEventListener
    {
        private WebClientController _webController;
        private Accounts_Server.Models.Sender _sender;
        private Accounts_Server.Models.Account _account = null;

        public Form1()
        {
            InitializeComponent();
            _webController = new WebClientController(this);
            
            _sender = new Sender
            {
                AppVersion = "0.1",
                AppType = Const.ClientAppType,
                IpAddress = Const.GetAddresses().Where(a => a.ToString().Contains("192.168.1.")).ToString(),
                Name = Environment.MachineName
            };
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var api = new Accounts_Server.Web_Space.Model.ApiMessage
            {
                Code = 200,
                JsonObject = null,
                RequestType = "GetAccount",
                StringMessage = "GameComputer",
                JsonSender = _sender.ToJson()
            };
            var result = await _webController.SendPostDataAsync(api);
            if (result == null) return;
            api = result.ParseJson<Accounts_Server.Web_Space.Model.ApiMessage>();
            if (api.JsonObject == null)
            {
                OnMessage(api.StringMessage);
                return;
            }
            _account = api.JsonObject.ParseJson<Account>();
            OnMessage($"Received _account = {_account}");
        }

        private void DisplayText(string text)
        {
            textBox1.Text += $"[{DateTime.Now}] {text}\r\n";
        }

        public void OnException(Exception ex)
        {
            DisplayText(ex.Message);
        }

        public void OnMessage(string message)
        {
            DisplayText(message);
        }

        public void UpdateAccountCount(int count, int available)
        {
            throw new NotImplementedException();
        }

        public void OnSettingsChanged(Settings settings)
        {
            throw new NotImplementedException();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var api = new Accounts_Server.Web_Space.Model.ApiMessage
            {
                Code = 200,
                JsonObject = _account.ToJson(),
                RequestType = "ReleaseAccount",
                StringMessage = "GameComputer",
                JsonSender = _sender.ToJson()
            };
            var result = await _webController.SendPostDataAsync(api);
            if (result == null) return;
            api = result.ParseJson<Accounts_Server.Web_Space.Model.ApiMessage>();
            if (api.JsonObject == null)
            {
                OnMessage(api.StringMessage);
                return;
            }
            //_account = api.JsonObject.ParseJson<Accounts_Server.Models.Account>();
            OnMessage($"Sent api = {api}");
        }
    }
}
