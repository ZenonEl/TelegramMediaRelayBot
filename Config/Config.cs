using System.Resources;
using Serilog.Events;
using Microsoft.Extensions.Configuration;


namespace TelegramMediaRelayBot
{
    class Config
    {
        public static TGBot? bot;
        public static string? telegramBotToken;
        public static string? sqlConnectionString;
        public static string databaseName = "TelegramMediaRelayBot";
        public static string? language;
        public static string proxy = "";
        public static int userUnMuteCheckInterval = 20; // Seconds
        public static bool isUseGalleryDl = false;

        public static int videoGetDelay = 1000;
        public static int contactSendDelay = 1000;

        public static LogEventLevel logLevel = LogEventLevel.Information;
        public static bool showVideoDownloadProgress = false;
        public static bool showVideoUploadProgress = false;

        public static bool torEnabled = false;
        public static string? torControlPassword;
        public static string? torSocksHost;
        public static int torSocksPort = 9050;
        public static int torControlPort = 9051;
        public static int torChangingChainInterval = 5; // Minutes

        private static bool isAccessPolicyEnabled = true;
        private static bool isAccessNewUsersEnabled = true;
        public static bool showAccessDeniedMessage = false;
        private static bool isAllowNewUsers = true;
        private static bool isAllowAll = false;
        private static List<long>? whitelistedReferrerIds = [];
        private static List<long>? blacklistedReferrerIds = [];

        private static ResourceManager resourceManager = new ResourceManager("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);
        public static void LoadConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            telegramBotToken = configuration["AppSettings:TelegramBotToken"]!;
            sqlConnectionString = configuration["AppSettings:SqlConnectionString"]!;
            databaseName = configuration["AppSettings:DatabaseName"]!;
            language = configuration["AppSettings:Language"]!;
            proxy = configuration["AppSettings:Proxy"]!;
            isUseGalleryDl = bool.Parse(configuration["GalleryDlSettings:Enabled"]!);

            videoGetDelay = int.Parse(configuration["MessageDelaySettings:VideoGetDelay"]!);
            contactSendDelay = int.Parse(configuration["MessageDelaySettings:ContactSendDelay"]!);

            logLevel = Enum.Parse<LogEventLevel>(configuration["ConsoleOutputSettings:LogLevel"]!, true);
            showVideoDownloadProgress = bool.Parse(configuration["ConsoleOutputSettings:ShowVideoDownloadProgress"]!);
            showVideoUploadProgress = bool.Parse(configuration["ConsoleOutputSettings:ShowVideoUploadProgress"]!);

            torEnabled = bool.Parse(configuration["Tor:Enabled"]!);
            torControlPassword = configuration["Tor:TorControlPassword"];
            torSocksHost = configuration["Tor:TorSocksHost"];
            torSocksPort = int.Parse(configuration["Tor:TorSocksPort"]!);
            torControlPort = int.Parse(configuration["Tor:TorControlPort"]!);

            isAccessPolicyEnabled = bool.Parse(configuration["AccessPolicy:Enabled"]!);
            isAccessNewUsersEnabled = bool.Parse(configuration["AccessPolicy:NewUsersPolicy:Enabled"]!);
            showAccessDeniedMessage = bool.Parse(configuration["AccessPolicy:NewUsersPolicy:ShowAccessDeniedMessage"]!);
            isAllowNewUsers = bool.Parse(configuration["AccessPolicy:NewUsersPolicy:AllowNewUsers"]!);
            isAllowAll = bool.Parse(configuration["AccessPolicy:NewUsersPolicy:AllowRules:AllowAll"]!);
            whitelistedReferrerIds = configuration.GetSection("AccessPolicy:NewUsersPolicy:AllowRules:WhitelistedReferrerIds").Get<List<long>>();
            blacklistedReferrerIds = configuration.GetSection("AccessPolicy:NewUsersPolicy:AllowRules:BlacklistedReferrerIds").Get<List<long>>();
        }

        public static string GetResourceString(string key)
        {
            return resourceManager.GetString(key)!;
        }

        public static bool CanUserStartUsingBot(string referrerLink)
        {
            if (!isAccessPolicyEnabled) return true;

            long referrerUserId = DBforGetters.GetTelegramIdByLink(referrerLink);
            if (referrerUserId == -1) return false;

            bool isReferrerBlacklisted = blacklistedReferrerIds?.Contains(referrerUserId) ?? false;
            bool isReferrerWhitelisted = whitelistedReferrerIds?.Contains(referrerUserId) ?? false;

            return (isAllowAll && !isReferrerBlacklisted) ||
                (isAccessNewUsersEnabled && isAllowNewUsers && isReferrerWhitelisted);
        }
    }
}
