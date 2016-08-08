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
using Next.Accounts_Server.Database_Namespace;
using Next.Accounts_Server.Models;

namespace Next.Accounts_Server.Windows
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindows.xaml
    /// </summary>
    public partial class SettingsWindows : Window, IEventListener
    {
        private IEventListener _listener;
        private IDatabase _database;

        private Settings _source;

        public SettingsWindows(IDatabase database, Settings source, IEventListener listener)
        {
            InitializeComponent();
            _source = source;
            _listener = listener;
            _database = database;
            Test();
        }

        private async void Test()
        {
            PostgresDatabase pg = new PostgresDatabase(_source, this);
            var check = await pg.CheckConnection();
            var centerName = "test_club";
            var accounts = await pg.GetAccounts($"WHERE club='{centerName}'");
            if (accounts == null) return;

            var restored = await _database.RestoreAccounts(accounts);
            OnMessage($"Restored {restored} of accounts");
        }

        public void OnException(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        public void OnMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
