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
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;


namespace TelegramMediaRelayBot.TelegramBot;

class Scheduler
{
    private Timer? _unMuteTimer;
    private Timer? _torChangingChainTimer;
    private readonly IUserRepository _userRepository;
    private readonly IUserGetter _userGetter;
    private readonly IOptionsMonitor<MessageDelayConfiguration> _delayConfig;
    private readonly IOptionsMonitor<TorConfiguration> _torConfig;
    private readonly object _timerLock = new();
    private int? _lastUnmuteIntervalSeconds;
    private bool? _lastTorEnabled;
    private int? _lastTorIntervalMinutes;

    public Scheduler(
        IUserRepository userRepository,
        IUserGetter userGetter,
        IOptionsMonitor<MessageDelayConfiguration> delayConfig,
        IOptionsMonitor<TorConfiguration> torConfig
    )
    {
        _userRepository = userRepository;
        _userGetter = userGetter;
        _delayConfig = delayConfig;
        _torConfig = torConfig;

        // Subscribe to config changes to apply hot reload safely
        _delayConfig.OnChange(_ => ReconfigureUnmuteTimer());
        _torConfig.OnChange(_ => ReconfigureTorTimer());
    }

    public void Init()
    {
        var unmutePeriod = TimeSpan.FromSeconds(_delayConfig.CurrentValue.UserUnMuteCheckInterval);
        _unMuteTimer = new Timer(async _ => await CheckForUnmuteContacts(), null, TimeSpan.Zero, unmutePeriod);
        _lastUnmuteIntervalSeconds = _delayConfig.CurrentValue.UserUnMuteCheckInterval;

        if (_torConfig.CurrentValue.Enabled)
        {
            var torPeriod = TimeSpan.FromMinutes(_torConfig.CurrentValue.TorChangingChainInterval);
            _torChangingChainTimer = new Timer(async _ => await TorChangingChain(), null, TimeSpan.Zero, torPeriod);
        }
        _lastTorEnabled = _torConfig.CurrentValue.Enabled;
        _lastTorIntervalMinutes = _torConfig.CurrentValue.TorChangingChainInterval;
        Log.Information("Scheduler started");
    }

    private void ReconfigureUnmuteTimer()
    {
        try
        {
            var newInterval = _delayConfig.CurrentValue.UserUnMuteCheckInterval;
            lock (_timerLock)
            {
                if (_lastUnmuteIntervalSeconds == newInterval)
                {
                    return; // no-op
                }
                _lastUnmuteIntervalSeconds = newInterval;

                var period = TimeSpan.FromSeconds(newInterval);
                if (_unMuteTimer == null)
                {
                    _unMuteTimer = new Timer(async _ => await CheckForUnmuteContacts(), null, period, period);
                }
                else
                {
                    // при переконфигурировании не запускаем немедленно, чтобы избежать дубликатов
                    _unMuteTimer.Change(period, period);
                }
            }
            Log.Information("Applied hot config [MessageDelaySettings]: UserUnMuteCheckInterval -> {Seconds}s", newInterval);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply hot config for unmute timer");
        }
    }

    private void ReconfigureTorTimer()
    {
        try
        {
            var enabled = _torConfig.CurrentValue.Enabled;
            var interval = _torConfig.CurrentValue.TorChangingChainInterval;
            lock (_timerLock)
            {
                if (_lastTorEnabled == enabled && _lastTorIntervalMinutes == interval)
                {
                    return; // no-op
                }
                _lastTorEnabled = enabled;
                _lastTorIntervalMinutes = interval;

                if (!enabled)
                {
                    _torChangingChainTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    _torChangingChainTimer?.Dispose();
                    _torChangingChainTimer = null;
                    Log.Information("Applied hot config [Tor]: Enabled -> false (timer stopped)");
                    return;
                }

                var period = TimeSpan.FromMinutes(interval);
                if (_torChangingChainTimer == null)
                {
                    _torChangingChainTimer = new Timer(async _ => await TorChangingChain(), null, period, period);
                }
                else
                {
                    // при переконфигурировании не запускаем немедленно, чтобы избежать дубликатов
                    _torChangingChainTimer.Change(period, period);
                }
            }
            Log.Information("Applied hot config [Tor]: Enabled -> true, TorChangingChainInterval -> {Minutes}m", interval);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply hot config for tor timer");
        }
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

    private async Task TorChangingChain()
    {
        try
        {
            var controlPortClient = new DotNetTor.ControlPort.Client(
                _torConfig.CurrentValue.TorSocksHost,
                controlPort: _torConfig.CurrentValue.TorControlPort,
                password: _torConfig.CurrentValue.TorControlPassword ?? "");

            await controlPortClient.ChangeCircuitAsync();

            using (var httpClient = new HttpClient(new SocksPortHandler(
                _torConfig.CurrentValue.TorSocksHost,
                socksPort: _torConfig.CurrentValue.TorSocksPort)))
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
