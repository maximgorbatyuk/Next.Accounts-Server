using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Next.Accounts_Client.App_data;
using Next.Accounts_Client.Controllers;
using Next.Accounts_Client.Controllers.Realize_Classes;
using Next.Accounts_Client.Web_Space;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Controllers;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Client
{
    public partial class Form1 : Form, IListener, ITrackerListener, IResponseListener
    {
        private IRequestSender _requestSender;
        private IProcessLauncher _processLauncher;
        private ProcessTracker _processTracker;
        private IUsingTracker _usingTracker;
        private ILogger _logger;
        private App_data.Settings _settings;
        private Sender _sender;
        private Account _account = null;
        private readonly string[] _arguments = null;
        private string _gameCode = null;
        private bool _connectionActive = false;

        public Form1(string[] args)
        {
            InitializeComponent();
            InitSettings();
            _arguments = args;
        }

        private async void InitSettings()
        {
            var stringSettings = await IoController.ReadFileAsync(Const.SettingsFilename);
            if (stringSettings == null)
            {
                _settings = new App_data.Settings();
                IoController.WriteToFileAsync(Const.SettingsFilename, _settings.ToJson());
            }
            else { _settings = stringSettings.ParseJson<App_data.Settings>(); }


            _sender = Const.GetSender();
            _logger = new DefaultLogger();
            
            _requestSender = new WebClientController(this, this);
            _processLauncher = new DefaultProcessLauncher(this);
            _processTracker = new ProcessTracker(_settings)
            {
                EventListener = this,
                ProcessLauncher = _processLauncher,
                TrackerListener = this
            };
            _usingTracker = new DefaultUsingTracker(_requestSender, _sender);
        }

        private async void RequestAccount()
        {
            StartProgressBar();
            var api = new ApiMessage
            {
                Code = 200,
                JsonObject = null,
                RequestType = Const.RequestTypeGet,
                StringMessage = "GameComputer",
                JsonSender = _sender.ToJson()
            };
            _connectionActive = true;
            var result = await _requestSender.SendPostDataAsync(api);
            
        }

        private void button1_Click(object sender, EventArgs e) => RequestAccount();

        private void DisplayText(string text) => LogTextBox.Text += $"[{DateTime.Now}] {text}\r\n";

        private void DisplayInMainlabel(string text) => MainLabel.Text = text;

        public void OnEvent(string message, object sender = null)
        {
            DisplayText(message);
            _logger.Log(message);
        } 

        public void OnException(Exception ex)
        {
            DisplayText(ex.Message);
            _logger.LogError(ex.Message);
        }

        private void button2_Click(object sender, EventArgs e) => ReleaseAccount();

        private async void ReleaseAccount()
        {
            var api = new ApiMessage
            {
                Code = 200,
                JsonObject = _account.ToJson(),
                RequestType = Const.RequestTypeRelease,
                StringMessage = "GameComputer",
                JsonSender = _sender.ToJson()
            };
            _account = null;
            _connectionActive = true;
            var result = await _requestSender.SendPostDataAsync(api);
            
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void Notify(string message, string title = "Steam launcher", int timeout = 3)
        {
            notifyIcon.ShowBalloonTip(timeout, title, message, ToolTipIcon.Info);
        }

        public void LaunchSteam(Account account, string applicationCode)
        {
            var info = new ProcessStartInfo
            {
                FileName = _settings.SteamDirectory,
                Arguments = $"-applaunch {applicationCode} -login {account.Login} {account.Password}"
            };
            var result = _processLauncher.StartProcess(info);
            if (!result) OnSteamClosed();
        }

        public void OnAccountReleased(Account account)
        {
            throw new NotImplementedException();
        }

        public void OnSteamStarted()
        {
            WindowState = FormWindowState.Minimized;
            //notifyIcon.Visible = true;
            Notify("Steam запущен");
        }

        public void OnSteamClosed()
        {
            ReleaseAccount();
            Notify("Steam закрыт");
            CloseApplication();
        }

        public void OnServerResponse(string responseString)
        {
            _connectionActive = false;
            var apiResponse = responseString.ParseJson<ApiMessage>();
            if (apiResponse == null)
            {
                DisplayText($"Received null apiResponse: {responseString}");
                DisplayInMainlabel(_settings.BadConnectionMessage);
                return;
            }
            string displayMessage = null;
            var requestType = Const.GetRequestType(apiResponse);
            if (apiResponse.Code == 404)
            {
                displayMessage = $"Received responseCode={apiResponse.Code}. String message: {apiResponse.StringMessage}";
                DisplayText(displayMessage);
                if (requestType == ApiRequests.GetAccount)
                {
                    DisplayInMainlabel(_settings.NoAvailableAccountsMessage);
                }
                return;
            }
            string jsonObject = null;
            Account account = null;
            var sender = apiResponse.JsonSender.ParseJson<Sender>();
            switch (requestType)
            {
                case ApiRequests.GetAccount:
                    jsonObject = apiResponse.JsonObject;
                    account = jsonObject?.ParseJson<Account>();
                    _account = account;
                    displayMessage = account != null ? $"Account {_account} received. Sender {sender}" : $"null account data";
                    DisplayInMainlabel(_settings.OkayMessage);
                    _usingTracker.SetAccount(_account);
                    // Launch steam if an account has been received
                    LaunchSteam(_account, _gameCode);
                    break;

                case ApiRequests.ReleaseAccount:
                    jsonObject = apiResponse.JsonObject;
                    account = jsonObject?.ParseJson<Account>();
                    _account = null;
                    displayMessage = account != null ?
                        $"Account {account} has been released. Sender {sender}" :
                        $"null account data while ReleaseAccount processing";
                    DisplayInMainlabel(_settings.ReleasedMessage);
                    _usingTracker.ClearAccount();
                    // Closing the application if the account has been released
                    CloseApplication();
                    break;

                case ApiRequests.UsingAccount:
                    jsonObject = apiResponse.JsonObject;
                    account = jsonObject?.ParseJson<Account>();
                    if (account != null && _account == null) _account = account;

                    displayMessage = account != null ?
                        $"Account {_account} time has been reset. Sender {sender}" :
                        $"null account data while UsingAccount processing";
                    break;

                case ApiRequests.Unknown:
                case ApiRequests.None:
                default:
                    displayMessage = "No ways inside SWITCH statement. Sender {sender}";
                    break;
            }
            DisplayText(displayMessage);
            StopProgressBar();
            OkButton.Enabled = true;
        }

        public void OnConnectionError(Exception ex)
        {
            DisplayInMainlabel(_settings.BadConnectionMessage);
            StopProgressBar();
            OkButton.Enabled = true;
        }

        private void progresBarTimer_Tick(object sender, EventArgs e)
        {
            var curValue = progressBar.Value;
            curValue += 5;
            if (curValue >= progressBar.Maximum)
            {
                StartProgressBar();
                return;
            }
            progressBar.Value = curValue;
        } 

        private void StartProgressBar()
        {
            progressBar.Value = progressBar.Minimum;
            progresBarTimer.Enabled = true;
        }

        private void StopProgressBar()
        {
            progressBar.Value = progressBar.Maximum;
            progresBarTimer.Enabled = false;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            OkButton.Enabled = false;
            _processLauncher.CloseProcesses(_settings.ProcessName);
            _gameCode = _arguments != null && _arguments.Length == 2 ? _arguments[0] : "0";
            var title = _arguments != null && _arguments.Length == 2 ? _arguments[1] : "Steam launcher";
            this.Text = title;
            RequestAccount();
        }

        private void CloseApplication()
        {
            Application.Exit();
        }

        private void MainButton(object sender, EventArgs e)
        {
            CloseApplication();
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Normal;
            //notifyIcon.Visible = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var reason = e.CloseReason;
            if (reason == CloseReason.WindowsShutDown)
            {
                if (_account != null) ReleaseAccount();
                _processLauncher.CloseProcesses(_settings.ProcessName);
                return;
            }
            if (reason == CloseReason.ApplicationExitCall)
            {
                if (_account == null)
                {
                    _processLauncher.CloseProcesses(_settings.ProcessName);
                    return;
                }
            }
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
        }
    }
}
