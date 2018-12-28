using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPPCore.Database
{
    public class MemoryDataProvider : IDataProvider
    {
        private Dictionary<int, Tuple<string, DateTime>> memory = new Dictionary<int, Tuple<string, DateTime>> { };
        private int Counter = 1;

        public MemoryDataProvider(string Database, string Host, string ApplicationName, string Username, string Password, int Port)
        {
        }
#pragma warning disable 1998
        public async Task ExecuteCommand(string command, IDbParameter[] parameters = null)
        {
            string action = command.Split(' ')[0];
            switch (action)
            {
                case "removeall":
                    memory.Clear();
                    break;
                case "remove":
                    try
                    {
                        int.TryParse(command.Substring(6), out int result);
                        memory.Remove(result);
                        break;
                    } catch
                    {
                        break;
                    }
                case "insert":
                    DateTime dateTime = DateTime.UtcNow;
                    memory.Add(Counter, new Tuple<string, DateTime>(command.Substring(6).Trim(), dateTime));
                    Counter++;
                    break;
                default:
                    break;
            }
        }

        public async Task<object[]> GetDataFromCommand(string command, IDbParameter[] parameters = null)
        {
            string action = command.Split(' ')[0];
            switch (action)
            {
                case "record":
                    try
                    {
                        int.TryParse(command.Substring(8), out int result);
                        memory.TryGetValue(result, out Tuple<string, DateTime> value);
                        return new object[] { result, value.Item1, value.Item2 };
                    } catch
                    {
                        return new object[] { };
                    }
                case "maxkey":
                    return new object[] { memory.Keys.Max() };
                default:
                    return new object[] { };
            }
        }
#pragma warning restore 1998
    }
}
