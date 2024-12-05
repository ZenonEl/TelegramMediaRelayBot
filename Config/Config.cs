using System;
using Microsoft.Extensions.Configuration;

namespace TikTokMediaRelayBot
{
    class Config
    {
        static ConfigurationBuilder configuration;
        public static string telegramBotToken;
        public static string sqlConnectionString;
        public static void loadConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            telegramBotToken = configuration["AppSettings:TelegramBotToken"];
            sqlConnectionString = configuration["AppSettings:SqlConnectionString"];
            
        }
    }
}
