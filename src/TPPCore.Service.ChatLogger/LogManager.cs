using System;
using System.IO;
using TPPCore.Service.Common;
using Newtonsoft.Json;
using TPPCore.Service.Chat.DataModels;

namespace TPPCore.Service.ChatLogger
{
	public class LogManager
    {
        public static string LogLocation;
        public static string FilePath;
        public static DateTime dateTime;

        public static void Configure(ServiceContext context)
        {
            LogLocation = context.ConfigReader.GetCheckedValue<string>("log", "path");
            if (LogLocation == null)
                LogLocation = "logs/";
        }

        public static void LogMessage(string message)
        {
            dateTime = DateTime.UtcNow;
            RawContentEvent converted = JsonConvert.DeserializeObject<RawContentEvent>(message);
            FilePath = LogLocation + converted.ClientName + dateTime.Date.ToString("yyyy'-'MM'-'dd") + ".log";
            CreateLogPath();

            File.AppendAllText(FilePath, dateTime.ToString("o") + " " + converted.RawContent + Environment.NewLine);
        }

        public static void CreateLogPath()
        {
            if (!Directory.Exists(LogLocation))
                Directory.CreateDirectory(LogLocation);
        }
    }
}
