// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace TelegramMediaRelayBot.Config.Validation;

public class DownloaderSettingsConfigurationValidator : IValidateOptions<DownloaderSettingsConfiguration>
{
    public ValidateOptionsResult Validate(string? name, DownloaderSettingsConfiguration options)
    {
        if (options.MaxFrequentDomains <= 0)
        {
            return ValidateOptionsResult.Fail("AppSettings:DownloaderSettings:MaxFrequentDomains must be > 0.");
        }
        if (options.CacheTimeout <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail("AppSettings:DownloaderSettings:CacheTimeout must be positive.");
        }
        if (string.IsNullOrWhiteSpace(options.ConfigFilePath))
        {
            return ValidateOptionsResult.Fail("AppSettings:DownloaderSettings:ConfigFilePath must be provided.");
        }
        return ValidateOptionsResult.Success;
    }
}

