// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using Microsoft.Extensions.Options;

namespace TelegramMediaRelayBot.Config.Validation;

public class TorConfigurationValidator : IValidateOptions<TorConfiguration>
{
    public ValidateOptionsResult Validate(string? name, TorConfiguration options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }
        if (string.IsNullOrWhiteSpace(options.TorSocksHost))
        {
            return ValidateOptionsResult.Fail("Tor:TorSocksHost must be provided when Tor is enabled.");
        }
        if (options.TorSocksPort <= 0 || options.TorSocksPort > 65535)
        {
            return ValidateOptionsResult.Fail("Tor:TorSocksPort is invalid.");
        }
        if (options.TorControlPort <= 0 || options.TorControlPort > 65535)
        {
            return ValidateOptionsResult.Fail("Tor:TorControlPort is invalid.");
        }
        return ValidateOptionsResult.Success;
    }
}

