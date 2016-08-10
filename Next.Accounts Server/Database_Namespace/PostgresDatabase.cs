using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Next.Accounts_Server.Application_Space;
using Next.Accounts_Server.Models;
using Npgsql;

namespace Next.Accounts_Server.Database_Namespace
{
    public class PostgresDatabase : IRemoteStorage
    {

        private readonly NpgsqlConnection _connection;

        private readonly IEventListener _listener;

        private Settings _settings;

        public PostgresDatabase(Settings settings, IEventListener listener)
        {
            _settings = settings;
            _listener = listener;
            _connection = new NpgsqlConnection(settings.PostgresConnectionString);
        }

        public async Task<List<Account>> GetAccounts(string predicate = null)
        {
            var query = $"select * from steamusers";
            if (predicate != null) query += $" {predicate}";
            query += " order by id";
            var dt = await GetQueryResultAsync(query);

            if (dt == null) return null;

            List<Account> result = null;
            if (dt.Rows.Count == 1) return null;
            result = new List<Account>();

            foreach (DataRow row in dt.Rows)
            {
                var id = int.Parse(row[0].ToString());
                var userName = row[1].ToString();
                var userPassword = row[2].ToString();
                var availability = int.Parse(row[3].ToString()) == 1;
                var centerName = row[4].ToString();
                var account = new Account
                {
                    Id = id,
                    Login = userName,
                    Password = userPassword,
                    Available = availability,
                    CenterOwner = centerName,
                    ComputerName = ""
                };
                result.Add(account);
            }

            return result;
        }

        public async Task<bool> CheckConnection()
        {
            var query = "SELECT * FROM pg_catalog.pg_tables";
            var result = await GetQueryResultAsync(query);
            return result != null;

        }

        public void CloseDatabase()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }

        private async Task<int> ExecuteQueryAsync(string query)
        {
            CloseDatabase();
            var command = new NpgsqlCommand(query, _connection);
            try
            {
                _connection.Open();
                var count = await command.ExecuteNonQueryAsync();
                
                return count;
            }
            catch (NpgsqlException ex) { _listener.OnException(ex); }
            catch (Exception ex) { _listener.OnException(ex); }
            finally { _connection.Close();}
            return 0;
        }
        private async Task<DataTable> GetQueryResultAsync(string query)
        {
            //CloseDatabase();
            var command = new NpgsqlCommand(query, _connection);
            DataTable result = null;
            try
            {
                CloseDatabase();
                _connection.Open();
                var reader = await command.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    result = new DataTable();
                    result.Load(reader);
                }
                
            }
            catch (NpgsqlException ex) { _listener.OnException(ex); }
            catch (Exception ex) { _listener.OnException(ex); }
            finally { _connection.Close(); }
            return result;
        }
        private async Task<object> GetExecuteScalarAsync(string query)
        {
            CloseDatabase();
            var command = new NpgsqlCommand(query, _connection);
            object result = null;
            try
            {
                _connection.Open();
                result = await command.ExecuteScalarAsync();
                
            }
            catch (NpgsqlException ex) { _listener.OnException(ex); }
            catch (Exception ex) { _listener.OnException(ex); }
            finally { _connection.Close(); }
            return result;
        }
    }
}