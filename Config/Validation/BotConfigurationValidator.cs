// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace TelegramMediaRelayBot.Config.Validation;

public class BotConfigurationValidator : IValidateOptions<BotConfiguration>
{
    public ValidateOptionsResult Validate(string? name, BotConfiguration options)
    {
        if (string.IsNullOrWhiteSpace(options.TelegramBotToken))
        {
            return ValidateOptionsResult.Fail("AppSettings:TelegramBotToken must be provided.");
        }
        if (string.IsNullOrWhiteSpace(options.DatabaseType))
        {
            return ValidateOptionsResult.Fail("AppSettings:DatabaseType must be provided (sqlite or mysql).");
        }
        string type = options.DatabaseType.ToLowerInvariant();
        if (type != "sqlite" && type != "mysql")
        {
            return ValidateOptionsResult.Fail("AppSettings:DatabaseType must be either 'sqlite' or 'mysql'.");
        }
        if (type == "mysql" && string.IsNullOrWhiteSpace(options.SqlConnectionString))
        {
            return ValidateOptionsResult.Fail("For MySQL, AppSettings:SqlConnectionString must be provided.");
        }
        return ValidateOptionsResult.Success;
    }
}

