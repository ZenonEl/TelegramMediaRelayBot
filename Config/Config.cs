// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Serilog.Events;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot
{
    /// <summary>
    /// Loads and validates <see cref="AppConfig"/> at startup and exposes the
    /// values. The flat static fields are a transition bridge for existing static
    /// consumers; new code should inject <see cref="AppConfig"/> instead.
    /// </summary>
    class Config
    {
        public static AppConfig Current { get; private set; } = new();

        // ---- transition bridge: flat views over Current, kept for existing callers ----
        public static string? telegramBotToken => Current.Bot.Token;
        public static string? telegramApiBaseUrl => Current.Bot.ApiBaseUrl;
        public static string? telegramApiProxy => Current.ResolveProxyUrl(Current.Bot.Proxy);
        public static string sqlConnectionString => Current.Database.ConnectionString;
        public static string databaseName => Current.Database.Name;
        public static string? language => Current.Bot.Language;
        public static int userUnMuteCheckInterval => Current.Session.UnMuteCheckIntervalSeconds;
        public static bool isUseGalleryDl => Current.Download.UseGalleryDl;
        public static string accessDeniedMessageContact => Current.Access.DeniedMessageContact;
        public static string? cookiesFromBrowser => Current.Download.CookiesFromBrowser;
        public static string? cookiesFile => Current.Download.CookiesFile;
        public static string proxy => Current.ResolveProxyUrl(Current.Download.DefaultProxy) ?? "";
        public static bool torEnabled => Current.Tor.Enabled;
        public static string? torSocksHost => Current.Tor.SocksHost;
        public static int torSocksPort => Current.Tor.SocksPort;
        public static int maxConcurrentDownloads => Current.Download.MaxConcurrent;
        public static int videoGetDelay => Current.Delays.VideoGetMs;
        public static int contactSendDelay => Current.Delays.ContactSendMs;
        public static int sessionTtlMinutes => Current.Session.TtlMinutes;
        public static int sessionCleanupIntervalMinutes => Current.Session.CleanupIntervalMinutes;
        public static LogEventLevel logLevel => Current.Logging.Level;
        public static bool showVideoDownloadProgress => Current.Logging.ShowDownloadProgress;
        public static bool showVideoUploadProgress => Current.Logging.ShowUploadProgress;
        public static bool showAccessDeniedMessage => Current.Access.ShowDeniedMessage;

        public static void LoadConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var config = new AppConfig();
            configuration.Bind(config);

            var problems = config.Validate().ToList();
            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    "Invalid configuration in appsettings.json:" + Environment.NewLine +
                    string.Join(Environment.NewLine, problems.Select(p => "  - " + p)));
            }

            Current = config;
        }

        public static ITelegramBotClient CreateTelegramBotClient()
        {
            if (string.IsNullOrWhiteSpace(Current.Bot.Token))
                throw new ArgumentException("Telegram Bot Token is not configured.");

            HttpClient? httpClient = null;
            string? apiProxy = Current.ResolveProxyUrl(Current.Bot.Proxy);
            if (!string.IsNullOrWhiteSpace(apiProxy))
            {
                var webProxy = new System.Net.WebProxy(apiProxy);
                var handler = new System.Net.Http.SocketsHttpHandler { Proxy = webProxy, UseProxy = true };
                httpClient = new HttpClient(handler);
            }

            var options = string.IsNullOrWhiteSpace(Current.Bot.ApiBaseUrl)
                ? new TelegramBotClientOptions(Current.Bot.Token)
                : new TelegramBotClientOptions(Current.Bot.Token, Current.Bot.ApiBaseUrl);

            return httpClient != null
                ? new TelegramBotClient(options, httpClient)
                : new TelegramBotClient(options);
        }

        public static bool CanUserStartUsingBot(string referrerLink, IUserGetter userGetter)
        {
            var access = Current.Access;
            if (!access.Enabled) return true;

            long referrerUserId = userGetter.GetUserTelegramIdByLink(referrerLink);
            if (referrerUserId == -1) return false;

            bool isReferrerBlacklisted = access.BlacklistedReferrerIds.Contains(referrerUserId);
            bool isReferrerWhitelisted = access.WhitelistedReferrerIds.Contains(referrerUserId);

            return (access.AllowAll && !isReferrerBlacklisted) ||
                (access.AllowNewUsers && isReferrerWhitelisted);
        }
    }
}
