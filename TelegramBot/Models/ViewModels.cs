// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.TelegramBot.Models;

public class ContactViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Link { get; init; }
    public required string MembershipInfo { get; init; }
}

public class GroupViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int MemberCount { get; init; }
    public bool IsDefault { get; init; }
}