using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TPPCore.Database
{
    public class PostgresqlDataProvider : IDataProvider
    {
        private NpgsqlConnection Connection;

        public PostgresqlDataProvider(string Database, string Host, string ApplicationName, string Username, string Password, int Port)
        {
            Connection = new NpgsqlConnection($"Host={Host};Username={Username};Database={Database};Port={Port.ToString()};Password={Password};Application Name={ApplicationName}");
        }

        /// <summary>
        /// Execute a non-returning command.
        /// </summary>
        /// <param name="command"></param>
        public async Task ExecuteCommand(string command)
        {
            if (Connection.State == System.Data.ConnectionState.Closed)
            {
                await Connection.OpenAsync();
            }

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand()
            {
                Connection = Connection,
                CommandText = command
            };
            await npgsqlCommand.ExecuteNonQueryAsync();

            if (Connection.State == System.Data.ConnectionState.Open)
            {
                Connection.Close();
            }
        }

        /// <summary>
        /// Execute a command that returns a value.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<string[]> GetDataFromCommand(string command)
        {
            if (Connection.State == System.Data.ConnectionState.Closed)
            {
                await Connection.OpenAsync();
            }

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand()
            {
                Connection = Connection,
                CommandText = command
            };
            NpgsqlDataReader reader = npgsqlCommand.ExecuteReader();
            List<string> results = new List<string> { };
            object[] values = new object[reader.FieldCount];
            reader.Read();
            reader.GetValues(values);
            foreach (object item in values)
            {
                results.Add(item.ToString());
            }
            if (Connection.State == System.Data.ConnectionState.Open)
            {
                Connection.Close();
            }
            return results.ToArray();
        }

    }
}
