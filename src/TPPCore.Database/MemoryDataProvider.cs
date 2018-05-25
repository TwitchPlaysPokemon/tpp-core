using System;
using System.Collections.Generic;
using System.Linq;

namespace TPPCore.Database
{
    public class MemoryDataProvider : DataProvider
    {
        private Dictionary<int, Tuple<string, string>> memory = new Dictionary<int, Tuple<string, string>> { };
        private int Counter = 1;

        public MemoryDataProvider(string Database, string Host, string ApplicationName, string Username, string Password, int Port)
        {
        }

        public override void ExecuteCommand(string command)
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

        public override string GetDataFromCommand(string command)
        {
            string action = command.Split(' ')[0];
            switch (action)
            {
                case "contents":
                    try
                    {
                        int.TryParse(command.Substring(8), out int result);
                        memory.TryGetValue(result, out Tuple<string, string> value);
                        return value.Item1;
                    } catch
                    {
                        return string.Empty;
                    }
                case "timestamp":
                    try
                    {
                        int.TryParse(command.Substring(9), out int result);
                        memory.TryGetValue(result, out Tuple<string, string> value);
                        return value.Item2;
                    } catch
                    {
                        return string.Empty;
                    }
                case "maxkey":
                    return memory.Keys.Max().ToString();
                default:
                    return string.Empty;
            }
        }
    }
}
