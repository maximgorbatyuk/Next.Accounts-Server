using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Extensions;
using Next.Accounts_Server.Models;
using static Next.Accounts_Server.Application_Space.Const;

namespace Next.Accounts_Server.Database_Namespace
{
    public class LiteDatabase : IDisposable, IDatabase
    {
        public string DatabaseName { get; set; } = "SteamAccounts.db3";

        private string _accountTableName = "steam_accounts";

        private readonly string _connectionString;

        private readonly SQLiteConnection _connection;

        private readonly IEventListener _eventListener;

        public LiteDatabase(IEventListener listener, string dbName = null)
        {
            _eventListener = listener;
            DatabaseName = dbName ?? DatabaseName;
            InitDirectories();
            var path = $"{Environment.CurrentDirectory}\\Databases\\{DatabaseName}";
            _connectionString = $"Data Source = {path}; Version=3;";
            _connection = new SQLiteConnection { ConnectionString = _connectionString };
            if (!File.Exists(path)) SQLiteConnection.CreateFile(DatabaseName);
            if (_connection.State == ConnectionState.Open) _connection.Close();
            InitiateTables();
        }

        public void InitDirectories()
        {
            var path = $"{Environment.CurrentDirectory}\\Databases\\";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = $"{Environment.CurrentDirectory}\\Logs\\";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public async void InitiateTables()
        {
//            var query = @"CREATE TABLE IF NOT EXISTS computers (
//                    id_computer INTEGER PRIMARY KEY,
//                    computer_name TEXT,
//                    client_version TEXT,
//                    last_used DATETIME)";
//            await ExecuteNonQueryAsync(query);

            var query = $"CREATE TABLE IF NOT EXISTS {_accountTableName} (" +
                        $"{IdColumn} INTEGER PRIMARY KEY AUTOINCREMENT," +
                        $"{LoginColumn} TEXT," +
                        $"{PasswordColumn} TEXT," +
                        $"{AvailableColumn} INTEGER," +
                        $"{ComputerNameColumn} TEXT)";
            var result = await ExecuteNonQueryAsync(query);
            Debug.WriteIf(result > 0, "Инициирована база аккаунтов");
        }

        private async Task<int> ExecuteNonQueryAsync(string query)
        {
            var result = 0;
            var command = new SQLiteCommand(query, _connection);
            try
            {
                _connection.Open();
                result = await command.ExecuteNonQueryAsync();
            }
            catch (SQLiteException ex)
            {
                _eventListener.OnException(ex);
            }
            finally
            {
                _connection.Close();
            }
            return result;
        }

        private async Task<DataTable> GetQueryResultAsync(string query)
        {
            var command = new SQLiteCommand(query, _connection);
            DataTable result = null;
            try
            {
                _connection.Open();
                var reader = await command.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    result = new DataTable();
                    result.Load(reader);
                }
                    
                _connection.Close();
            }
            catch (SQLiteException ex)
            {
                _eventListener.OnException(ex);
            }
            catch (Exception ex)
            {
                _eventListener.OnException(ex);
            }
            finally
            {
                _connection.Close();
            }
            return result;
        }

        private async void DeleteAccountsTable() => 
            await ExecuteNonQueryAsync("delete from steam_accounts where id>0");

        public void Dispose()
        {
            _connection.Dispose();
        }

        public async Task<Account> GetAccount(Computer computer, bool free = false)
        {
            Account account = null;
            var accounts = await GetListOfAccountsAsync(free);
            if (accounts.Count == 0) return account;
            account = free ? accounts.First(a => a.Available = true) : accounts.First();
            account.Available = false;
            account.ComputerName = computer != null ? computer.Name : NoComputer;
            await UpdateAccountAsync(account);
            return account;
        }

        public async Task<int> ReleaseAccount(Account account)
        {
            account.Available = true;
            account.ComputerName = "";
            return await UpdateAccountAsync(account);
        }

//        public async void UpdateComputer(Computer computer)
//        {
//            throw new NotImplementedException();
//        }

        public async Task<int> UpdateAccountAsync(Account account)
        {
            var query = $"UPDATE {_accountTableName} " +
                        $"SET {AvailableColumn}={account.Available.ToInt()}, " +
                        $"{ComputerNameColumn}='{account.ComputerName}' " +
                        $"WHERE {IdColumn}={account.Id}";
            return await ExecuteNonQueryAsync(query);
        }

        public async Task<List<Account>> GetListOfAccountsAsync(bool free = false)
        {
            var query = $"SELECT * FROM {_accountTableName}";
            var dt = await GetQueryResultAsync(query);
            List<Account> accounts = null;
            if (dt.Rows.Count <= 0) return null;
            accounts = new List<Account>();
            foreach (DataRow row in dt.Rows)
            {
                int id          = int.Parse(row[IdColumn].ToString());
                var login       = row[LoginColumn].ToString();
                var pass        = row[PasswordColumn].ToString();
                bool available  = int.Parse(row[AvailableColumn].ToString()) == 1;
                var computerName = row[ComputerNameColumn].ToString();
                accounts.Add(new Account
                {
                    Id = id,
                    Login = login,
                    Password = pass,
                    Available = available,
                    ComputerName = computerName
                });
            }
            return accounts;
        }

        public async Task<int> AddAccountsAsync(IList<Account> source)
        {
            if (source.Count == 0) return 0;
            var query = $"replace into {_accountTableName} " +
                        $"({IdColumn}, {LoginColumn}, {PasswordColumn}, {AvailableColumn}, {ComputerNameColumn}) values ";
            for (int index = 0; index < source.Count; index++)
            {
                var account = source[index];
                query += $"({account.Id}, '{account.Login}', '{account.Password}', {account.Available.ToInt()}, '{account.ComputerName}')";
                if (index != (source.Count - 1)) query += ", ";
            }
            var count = await ExecuteNonQueryAsync(query);
            return count;
        }

        public async Task<int> AddAccountAsync(Account account)
        {
            var list = new List<Account> { account };
            var result = await AddAccountsAsync(list);
            return result;
        }

        public async Task<int> RemoveAccountAsync(Account account)
        {
            var query = $"delete from {_accountTableName} where {IdColumn}={account.Id}";
            var result = await ExecuteNonQueryAsync(query);
            return result;
        }
    }
}