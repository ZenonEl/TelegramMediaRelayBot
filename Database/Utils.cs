// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Database;

public class Utils
{
    public static string GenerateUserLink()
    {
        return Guid.NewGuid().ToString();
    }

}
