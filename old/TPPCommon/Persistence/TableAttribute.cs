using System;
using System.Text.RegularExpressions;

namespace TPPCommon.Persistence
{
    /// <summary>
    /// Attribute used for giving a model the name of the logical unit it gets persisted in.
    /// For SQL this would equivalent to the table name, for MongoDB to the collection name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// Name of the logical unit.
        /// For SQL this is equivalent to the table name, for MongoDB to the collection name.
        /// </summary>
        public readonly string Table;

        /// <summary>
        /// Regex to restrict possible table names to a widely supported subset.
        /// In this case only lowercase, numbers, and underscores. 
        /// </summary>
        private const string NameRegex = @"^[a-z][a-z0-9_]*$";

        public TableAttribute(string table)
        {
            if (!Regex.IsMatch(table, NameRegex))
            {
                throw new ArgumentException($"table names must match this regex: '{NameRegex}'");
            }

            Table = table;
        }
    }
}