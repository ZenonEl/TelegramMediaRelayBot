using Microsoft.Extensions.Configuration;

namespace TikTokMediaRelayBot
{
    class Config
    {
        static ConfigurationBuilder configuration;
        public static string telegramBotToken;
        public static string sqlConnectionString;
        public static string inputId = "s_input";
        public static string downloadButtonClass = "btn-red";
        public static string finalDownloadButtonClass = "dl-success";
        public static string finalDownloadButtonID = "ConvertToVideo";
        public static int maxAttempts = 5;
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
