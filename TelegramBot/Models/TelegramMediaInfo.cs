// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Models;

public class TelegramMediaInfo
{
    public required string FileId { get; init; }
    public TelegramFileType Type { get; init; }
}
