using Npgsql;

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
        public void ExecuteCommand(string command)
        {
            Connection.Open();

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand()
            {
                Connection = Connection,
                CommandText = command
            };
            npgsqlCommand.ExecuteNonQuery();
            Connection.Close();
        }

        /// <summary>
        /// Execute a command that returns a value.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public string GetDataFromCommand(string command)
        {
            Connection.Open();

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand()
            {
                Connection = Connection,
                CommandText = command
            };
            NpgsqlDataReader reader = npgsqlCommand.ExecuteReader();
            string result = string.Empty;
            while (reader.Read())
            {
                result += reader.GetString(0);
            }
            Connection.Close();
            return result;
        }

    }
}
