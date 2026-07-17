// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Globalization;
using System.Resources;

namespace TelegramMediaRelayBot;

/// <summary>
/// Localized UI texts (EN/RU) backed by the .resx resources.
/// Resolution follows the current thread UI culture set at startup.
/// </summary>
public static class Localization
{
    private static readonly ResourceManager ResourceManager =
        new("TelegramMediaRelayBot.Resources.texts", typeof(Program).Assembly);

    public static string Get(string key) => ResourceManager.GetString(key)!;

    public static void SetCulture(string? language)
    {
        var culture = string.IsNullOrWhiteSpace(language)
            ? CultureInfo.CurrentUICulture
            : new CultureInfo(language);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
