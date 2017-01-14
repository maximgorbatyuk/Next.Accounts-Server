using System;
using System.Windows.Threading;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using Next.Accounts_Server.Web_Space;
using Next.Accounts_Server.Web_Space.Model;

namespace Next.Accounts_Client.Controllers.Realize_Classes
{
    public class DefaultUsingTracker : IUsingTracker
    {
        private readonly DispatcherTimer _timer;

        private readonly IRequestSender _requestSender;

        private Account _account;

        private ApiMessage _message;

        public DefaultUsingTracker(IRequestSender requestSender, Sender me, int min = 5)
        {
            _requestSender = requestSender;
            _account = null;
            _timer = new DispatcherTimer {Interval = TimeSpan.FromMinutes(min)};
            _timer.Tick += TimerOnTick;
            _message = new ApiMessage
            {
                Code = 200,
                JsonObject = null,
                JsonSender = me.ToJson(),
                RequestType = Const.RequestTypeUsing,
                StringMessage = "I still use account"
            };
        }

        private async void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (_requestSender == null || _account == null) return;
            
            _message.JsonObject = _account.ToJson();
            await _requestSender.SendPostDataAsync(_message);
        }

        public void StartTimer() => _timer.Start();

        public void StopTimer()
        {
            _timer.Stop();
        } 

        public void SetAccount(Account account)
        {
            _account = account;
            _message.JsonObject = _account.ToJson();
            StartTimer();
        }

        public void ClearAccount()
        {
            _account = null;
            StopTimer();
        }
    }
}