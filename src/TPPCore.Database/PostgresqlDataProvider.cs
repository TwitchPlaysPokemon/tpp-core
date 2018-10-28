using Npgsql;
using System.Linq;
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
        /// <param name="parameters"></param>
        public async Task ExecuteCommand(string command, IDbParameter[] parameters)
        {
            if (Connection.State == System.Data.ConnectionState.Closed)
            {
                await Connection.OpenAsync();
            }

            while (Connection.State != System.Data.ConnectionState.Open)
                await Task.Delay(1);

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand()
            {
                Connection = Connection,
                CommandText = command
            };

            PostgresqlParameter[] postgresqlParameters = (PostgresqlParameter[])parameters;

            foreach (PostgresqlParameter parameter in postgresqlParameters)
            {
                npgsqlCommand.Parameters.Add(parameter.parameterName, parameter.type);
            }

            while (Connection.State != System.Data.ConnectionState.Open)
                await Task.Delay(1);

            await npgsqlCommand.PrepareAsync();

            foreach (PostgresqlParameter parameter in postgresqlParameters)
            {
                var param = npgsqlCommand.Parameters.First(x => x.ParameterName == parameter.parameterName);
                param.Value = parameter.value;
            }

            while (Connection.State != System.Data.ConnectionState.Open)
                await Task.Delay(1);

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
        public async Task<object[]> GetDataFromCommand(string command, IDbParameter[] parameters)
        {
            if (Connection.State == System.Data.ConnectionState.Closed)
            {
                await Connection.OpenAsync();
            }

            while (Connection.State != System.Data.ConnectionState.Open)
                await Task.Delay(1);

            NpgsqlCommand npgsqlCommand = new NpgsqlCommand()
            {
                Connection = Connection,
                CommandText = command
            };

            PostgresqlParameter[] postgresqlParameters = (PostgresqlParameter[])parameters;

            foreach (PostgresqlParameter parameter in postgresqlParameters)
            {
                npgsqlCommand.Parameters.Add(parameter.parameterName, parameter.type);
            }

            while (Connection.State != System.Data.ConnectionState.Open)
                await Task.Delay(1);

            await npgsqlCommand.PrepareAsync();

            foreach (PostgresqlParameter parameter in postgresqlParameters)
            {
                var param = npgsqlCommand.Parameters.First(x => x.ParameterName == parameter.parameterName);
                param.Value = parameter.value;
            }


            while (Connection.State != System.Data.ConnectionState.Open)
                await Task.Delay(1);
            NpgsqlDataReader reader = npgsqlCommand.ExecuteReader();
            object[] values = new object[reader.FieldCount];
            reader.Read();
            reader.GetValues(values);
            if (Connection.State == System.Data.ConnectionState.Open)
            {
                Connection.Close();
            }
            return values;
        }

    }
}
