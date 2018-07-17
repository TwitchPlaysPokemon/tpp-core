using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPPCore.Database
{
    public class MemoryDataProvider : IDataProvider
    {
        private Dictionary<int, Tuple<string, string>> memory = new Dictionary<int, Tuple<string, string>> { };
        private int Counter = 1;

        public MemoryDataProvider(string Database, string Host, string ApplicationName, string Username, string Password, int Port)
        {
        }
#pragma warning disable 1998
        public async Task ExecuteCommand(string command)
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
                    memory.Add(Counter, new Tuple<string, string>(command.Substring(6).Trim(), dateTime.ToString("o")));
                    Counter++;
                    break;
                default:
                    break;
            }
        }

        public async Task<string[]> GetDataFromCommand(string command)
        {
            string action = command.Split(' ')[0];
            switch (action)
            {
                case "record":
                    try
                    {
                        int.TryParse(command.Substring(8), out int result);
                        memory.TryGetValue(result, out Tuple<string, string> value);
                        return new string[] { result.ToString(), value.Item1, value.Item2 };
                    } catch
                    {
                        return new string[] { };
                    }
                case "maxkey":
                    return new string[] { memory.Keys.Max().ToString() };
                default:
                    return new string[] { };
            }
        }
#pragma warning restore 1998
    }
}
