using System.Resources;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace TikTokMediaRelayBot
{
    class Config
    {
        public static string? telegramBotToken;
        public static string? sqlConnectionString;

        public static int videoGetDelay = 1000;
        public static int contactSendDelay = 1000;

        public static LogEventLevel logLevel = LogEventLevel.Information;
        public static bool showVideoDownloadProgress = false;
        public static bool showVideoUploadProgress = false;

        private static ResourceManager resourceManager = new ResourceManager("TikTokMediaRelayBot.Resources.texts", typeof(Program).Assembly);
        public static void LoadConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            telegramBotToken = configuration["AppSettings:TelegramBotToken"]!;
            sqlConnectionString = configuration["AppSettings:SqlConnectionString"]!;

            videoGetDelay = int.Parse(configuration["MessageDelaySettings:VideoGetDelay"]!);
            contactSendDelay = int.Parse(configuration["MessageDelaySettings:ContactSendDelay"]!);

            logLevel = Enum.Parse<LogEventLevel>(configuration["ConsoleOutputSettings:LogLevel"]!, true);
            showVideoDownloadProgress = bool.Parse(configuration["ConsoleOutputSettings:ShowVideoDownloadProgress"]!);
            showVideoUploadProgress = bool.Parse(configuration["ConsoleOutputSettings:ShowVideoUploadProgress"]!);
        }
        public static string GetResourceString(string key)
        {
            return resourceManager.GetString(key)!;
        }
    }
}
