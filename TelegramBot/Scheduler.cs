// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Sessions;


namespace TelegramMediaRelayBot.TelegramBot;

class Scheduler
{
    private Timer? _unMuteTimer;
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
        UserSessionManager.StartCleanupTimer();
        MediaSessionManager.StartCleanupTimer();
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
}
