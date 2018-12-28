using NpgsqlTypes;

namespace TPPCore.Database
{
    public class PostgresqlParameter : IDbParameter
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        public string parameterName;

        /// <summary>
        /// The type of the parameter
        /// </summary>
        public NpgsqlDbType type;

        /// <summary>
        /// The value of the parameter
        /// </summary>
        public object value;
    }
}
