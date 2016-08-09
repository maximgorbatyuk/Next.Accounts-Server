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

        private readonly IDatabaseListener _dbListener;

        private int _allCount = 0;

        private int _availableCount = 0;

        public LiteDatabase(IEventListener listener, IDatabaseListener dbListener, string dbName = null)
        {
            _eventListener = listener;
            _dbListener = dbListener;
            DatabaseName = dbName ?? DatabaseName;
            InitDirectories();
            var path = $"{Environment.CurrentDirectory}\\App_data\\{DatabaseName}";
            _connectionString = $"Data Source = {path}; Version=3;";
            _connection = new SQLiteConnection { ConnectionString = _connectionString };
            if (!File.Exists(path)) SQLiteConnection.CreateFile(DatabaseName);
            if (_connection.State == ConnectionState.Open) _connection.Close();
            InitiateTables();
        }

        public void InitDirectories()
        {
            var path = $"{Environment.CurrentDirectory}\\App_data\\";
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
                        $"{ComputerNameColumn} TEXT, " +
                        $"{CenterOwnerColumn} TEXT)";
            var result = await ExecuteNonQueryAsync(query);
            Debug.WriteIf(result > 0, "Инициирована база аккаунтов");
        }

        private async Task<int> ExecuteNonQueryAsync(string query)
        {
            var result = -1;
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

        private async Task<int> DeleteAccountsTable() => 
            await ExecuteNonQueryAsync($"delete from {_accountTableName} where id>0");

        public void Dispose()
        {
            _connection.Dispose();
        }

        public async Task<Account> GetAccount(Sender sender)
        {
            Account account = null;
            var accounts = await GetAccounts();
            if (accounts.Count == 0) return null;

            account = accounts.FirstOrDefault(a => a.Available == true);
            if (account == null) return null;
            account.Available = false;
            account.ComputerName = sender != null ? sender.Name : NoComputer;

            var availAc = accounts.Where(a => a.Available == true).ToList();
            _availableCount = availAc.Count;
            _allCount = accounts.Count;
            _dbListener.UpdateAccountCount(_allCount, _availableCount);

            var count = await UpdateAccountAsync(account);
            return account;
        }

        public async Task<int> ReleaseAccount(Account account)
        {
            account.Available = true;
            account.ComputerName = "";
            _availableCount++;
            _dbListener.UpdateAccountCount(_allCount, _availableCount);
            return await UpdateAccountAsync(account);
        }

        public async Task<int> UpdateAccountAsync(Account account)
        {
            var query = $"UPDATE {_accountTableName} " +
                        $"SET {AvailableColumn}={account.Available.ToInt()}, " +
                        $"{ComputerNameColumn}='{account.ComputerName}' " +
                        $"WHERE {IdColumn}={account.Id}";
            return await ExecuteNonQueryAsync(query);
        }

        public async Task<int> UpdateAccountAsync(IList<Account> accounts)
        {
            if (accounts.Count == 0) return 0;
            var count = 0;
            foreach (var a in accounts)
            {
                count += await UpdateAccountAsync(a);
            }
            return count;
        }

        public async Task<List<Account>> GetAccounts(bool all = true)
        {
            //var query = all ? $"SELECT * FROM {_accountTableName}" : $"SELECT * FROM {_accountTableName} WHERE {AvailableColumn}=0";
            var query = $"SELECT * FROM {_accountTableName}";
            var dt = await GetQueryResultAsync(query);
            List<Account> accounts = null;
            if (dt == null) return null;
            if (dt.Rows.Count == 0) return null;
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
            _allCount = accounts.Count;
            _availableCount = accounts.Count(a => a.Available == true);

            accounts = all ? accounts : accounts.Where(a => a.Available == false).ToList();
            _dbListener.UpdateAccountCount(_allCount, _availableCount);
            return accounts;
        }

        public async Task<int> AddAccountAsync(IList<Account> source)
        {
            if (source.Count == 0) return 0;
            var query = $"replace into {_accountTableName} " +
                        $"({IdColumn}, {LoginColumn}, {PasswordColumn}, {AvailableColumn}, {ComputerNameColumn}) values ";
            for (int index = 0; index < source.Count; index++)
            {
                var account = source[index];
                query += $"({account.Id}, '{account.Login}', '{account.Password}', {account.Available.ToInt()}, '{account.ComputerName}')";
                _allCount++;
                _availableCount++;
                if (index != (source.Count - 1)) query += ", ";
            }
            var count = await ExecuteNonQueryAsync(query);
            _dbListener.UpdateAccountCount(_allCount, _availableCount);
            return count;
        }

        public async Task<int> AddAccountAsync(Account account)
        {
            var list = new List<Account> { account };
            var result = await AddAccountAsync(list);
            return result;
        }

        public async Task<int> RemoveAccountAsync(Account account)
        {
            var query = $"delete from {_accountTableName} where {IdColumn}={account.Id}";
            var result = await ExecuteNonQueryAsync(query);
            _allCount--;
            _availableCount--;
            _dbListener.UpdateAccountCount(_allCount, _availableCount);
            return result;
        }

        public async Task<int> RestoreAccounts(IList<Account> source)
        {
            var count = await DeleteAccountsTable();
            if (count == -1) return -1;
            var result = await AddAccountAsync(source);
            _allCount = source.Count;
            _availableCount = source.Count(a => a.Available == true);
            _dbListener.UpdateAccountCount(_allCount, _availableCount);
            return result;
        }
    }
}