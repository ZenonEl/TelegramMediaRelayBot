// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Resources;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot
{
    class Config
    {
        public static string? telegramBotToken;
        public static string sqlConnectionString;
        public static string databaseName = "TelegramMediaRelayBot";
        public static string dbType = "mysql";
        public static string? language;
        public static string proxy = "";
        public static int userUnMuteCheckInterval = 20; // Seconds
        public static bool isUseGalleryDl = false;
        public static string accessDeniedMessageContact = " ";

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

                .AddJsonFile("appsettings.example.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)

                .AddEnvironmentVariables()
                .Build();

            telegramBotToken = configuration["AppSettings:TelegramBotToken"]!;
            sqlConnectionString = configuration["AppSettings:SqlConnectionString"]!;
            databaseName = configuration["AppSettings:DatabaseName"]!;
            language = configuration["AppSettings:Language"]!;
            proxy = configuration["AppSettings:Proxy"]!;
            dbType = configuration.GetValue("AppSettings:DatabaseType", "sqlite");
            isUseGalleryDl = configuration.GetValue("AppSettings:UseGalleryDl", false);
            accessDeniedMessageContact = configuration.GetValue("AppSettings:AccessDeniedMessageContact", " ");

            videoGetDelay = configuration.GetValue("MessageDelaySettings:VideoGetDelay", 1000);
            contactSendDelay = configuration.GetValue("MessageDelaySettings:ContactSendDelay", 1000);

            logLevel = configuration.GetValue("ConsoleOutputSettings:LogLevel", LogEventLevel.Information);
            showVideoDownloadProgress = configuration.GetValue("ConsoleOutputSettings:ShowVideoDownloadProgress", false);
            showVideoUploadProgress = configuration.GetValue("ConsoleOutputSettings:ShowVideoUploadProgress", false);

            torEnabled = configuration.GetValue("Tor:Enabled", false);
            torControlPassword = configuration.GetValue("Tor:TorControlPassword", "");
            torSocksHost = configuration.GetValue("Tor:TorSocksHost", "127.0.0.1");
            torSocksPort = configuration.GetValue("Tor:TorSocksPort", 9050);
            torControlPort = configuration.GetValue("Tor:TorControlPort", 9051);
            torChangingChainInterval = configuration.GetValue("Tor:TorChangingChainInterval", 5);

            isAccessPolicyEnabled = configuration.GetValue("AccessPolicy:Enabled", false);
            isAccessNewUsersEnabled = configuration.GetValue("AccessPolicy:NewUsersPolicy:Enabled", false);
            showAccessDeniedMessage = configuration.GetValue("AccessPolicy:NewUsersPolicy:ShowAccessDeniedMessage", false);
            isAllowNewUsers = configuration.GetValue("AccessPolicy:NewUsersPolicy:AllowNewUsers", true);
            isAllowAll = configuration.GetValue("AccessPolicy:NewUsersPolicy:AllowRules:AllowAll", true);

            whitelistedReferrerIds = configuration.GetSection("AccessPolicy:NewUsersPolicy:AllowRules:WhitelistedReferrerIds").Get<List<long>>() ?? new List<long>();
            blacklistedReferrerIds = configuration.GetSection("AccessPolicy:NewUsersPolicy:AllowRules:BlacklistedReferrerIds").Get<List<long>>() ?? new List<long>();
        }

        public static string GetResourceString(string key)
        {
            return resourceManager.GetString(key)!;
        }

        public static bool CanUserStartUsingBot(string referrerLink, IUserGetter userGetter)
        {
            if (!isAccessPolicyEnabled) return true;

            long referrerUserId = userGetter.GetUserTelegramIdByLink(referrerLink);
            if (referrerUserId == -1) return false;

            bool isReferrerBlacklisted = blacklistedReferrerIds?.Contains(referrerUserId) ?? false;
            bool isReferrerWhitelisted = whitelistedReferrerIds?.Contains(referrerUserId) ?? false;

            return (isAllowAll && !isReferrerBlacklisted) ||
                (isAccessNewUsersEnabled && isAllowNewUsers && isReferrerWhitelisted);
        }
    }
}
