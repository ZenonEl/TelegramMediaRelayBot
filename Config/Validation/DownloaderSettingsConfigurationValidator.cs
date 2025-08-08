// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

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

