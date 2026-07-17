// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

namespace TelegramMediaRelayBot.Database.Interfaces;

/// <summary>A pending media download persisted so it survives restarts.</summary>
public sealed record DownloadJob(
    string Id,
    long ChatId,
    string Url,
    string Caption,
    List<long>? TargetUserIds,
    bool IsGroupChat);

public interface IDownloadJobRepository
{
    void Add(DownloadJob job);
    void Remove(string id);
    List<DownloadJob> GetAll();
}
