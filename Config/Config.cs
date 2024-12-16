using System.Resources;
using Microsoft.Extensions.Configuration;

namespace TikTokMediaRelayBot
{
    class Config
    {
        public static string? telegramBotToken;
        public static string? sqlConnectionString;
        public static ResourceManager resourceManager = new ResourceManager("TikTokMediaRelayBot.Resources.texts", typeof(Program).Assembly);

        public static int maxAttempts = 5;
        public static void LoadConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            telegramBotToken = configuration["AppSettings:TelegramBotToken"]!;
            sqlConnectionString = configuration["AppSettings:SqlConnectionString"]!;
            
        }
    }
}
