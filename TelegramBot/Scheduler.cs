// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using DotNetTor.SocksPort;
using TelegramMediaRelayBot.Database.Interfaces;


namespace TelegramMediaRelayBot.TelegramBot;

class Scheduler
{
    private Timer? _unMuteTimer;
    private Timer? _torChangingChainTimer;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;

    public Scheduler(
        IUserRepository userRepository,
        IUserGetter userGetter
    )
    {
        _userRepository = userRepository;
        _userGetter = userGetter;
    }

    public void Init()
    {
        _unMuteTimer = new Timer(async _ => await CheckForUnmuteContacts(), null, TimeSpan.Zero, TimeSpan.FromSeconds(Config.userUnMuteCheckInterval));
        if (Config.torEnabled) _torChangingChainTimer = new Timer(async _ => await TorChangingChain(), null, TimeSpan.Zero, TimeSpan.FromMinutes(Config.torChangingChainInterval));
        Log.Information("Scheduler started");
    }

    private Task CheckForUnmuteContacts()
    {
        try
        {
            List<int> expiredMutes = _userGetter.GetExpiredUsersMutes();

            foreach (var muteUserId in expiredMutes)
            {
                _userRepository.UnMuteUserByMuteId(muteUserId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in the method{MethodName}", nameof(CheckForUnmuteContacts));
        }

        return Task.CompletedTask;
    }

    private static async Task TorChangingChain()
    {
        try
        {
            var controlPortClient = new DotNetTor.ControlPort.Client(Config.torSocksHost, controlPort: Config.torControlPort,
                                                                    password: Config.torControlPassword);

            await controlPortClient.ChangeCircuitAsync();

            using (var httpClient = new HttpClient(new SocksPortHandler(Config.torSocksHost, socksPort: Config.torSocksPort)))
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
}
