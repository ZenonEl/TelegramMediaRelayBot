// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Net;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.TelegramBot;

class Scheduler
{
    private Timer? _unMuteTimer;
    private Timer? _torChangingChainTimer;
    private readonly IUserGetter _userGetter;
    private readonly IContactRemover _contactRemover;

    public Scheduler(
        IUserGetter userGetter,
        IContactRemover contactRemover
    )
    {
        _userGetter = userGetter;
        _contactRemover = contactRemover;
    }

    public void Init()
    {
        _unMuteTimer = new Timer(async _ => await CheckForUnmuteContacts(), null, TimeSpan.Zero, TimeSpan.FromSeconds(Config.userUnMuteCheckInterval));
        if (Config.torEnabled) _torChangingChainTimer = new Timer(async _ => await TorChangingChain(), null, TimeSpan.Zero, TimeSpan.FromMinutes(Config.torChangingChainInterval));
        UserSessionManager.StartCleanupTimer();
        Log.Information("Scheduler started");
    }

    private async Task CheckForUnmuteContacts()
    {
        try
        {
            var expiredMutes = await _userGetter.GetExpiredMutesAsync();

            foreach (var (userId, contactId) in expiredMutes)
            {
                _contactRemover.UnmuteContact(userId, contactId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method{MethodName}", nameof(CheckForUnmuteContacts));
        }
    }

    private static async Task TorChangingChain()
    {
        try
        {
            await SendTorSignalNewnym();

            var proxy = new WebProxy($"socks5://{Config.torSocksHost}:{Config.torSocksPort}");
            var handler = new HttpClientHandler { Proxy = proxy, UseProxy = true };
            using (var httpClient = new HttpClient(handler))
            {
                var result = await httpClient.GetStringAsync("https://check.torproject.org/api/ip");
                Log.Debug("New Tor IP: " + result);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method{MethodName}", nameof(TorChangingChain));
        }
    }

    private static async Task SendTorSignalNewnym()
    {
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(Config.torSocksHost!, Config.torControlPort);
        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream) { AutoFlush = true };
        using var reader = new StreamReader(stream);

        string hexPassword = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(Config.torControlPassword ?? ""));
        await writer.WriteLineAsync($"AUTHENTICATE {hexPassword}");
        var authResponse = await reader.ReadLineAsync();
        if (authResponse == null || !authResponse.StartsWith("250"))
            throw new Exception($"Tor authentication failed: {authResponse}");

        await writer.WriteLineAsync("SIGNAL NEWNYM");
        var signalResponse = await reader.ReadLineAsync();
        if (signalResponse == null || !signalResponse.StartsWith("250"))
            throw new Exception($"Tor NEWNYM signal failed: {signalResponse}");
    }
}
