// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramMediaRelayBot.Domain.Models;

public sealed class UserSettings
{
    public DistributionSettings Distribution { get; set; } = new();
    public PrivacyUserSettings Privacy { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static UserSettings FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new(); }
        catch { return new(); }
    }
}

public sealed class DistributionSettings
{
    public string DefaultAction { get; set; } = "send_only_to_me";
    public string DefaultActionCondition { get; set; } = "";
    public int AutoSendDelaySeconds { get; set; } = 30;
    public List<int> TargetGroupIds { get; set; } = [];
    public List<int> TargetContactIds { get; set; } = [];
}

public sealed class PrivacyUserSettings
{
    public bool InboxEnabled { get; set; } = false;
    public bool AllowContentForwarding { get; set; } = true;
    public string WhoCanFindMe { get; set; } = "everyone";
    public SiteFilterSettings SiteFilter { get; set; } = new();
}

public sealed class SiteFilterSettings
{
    public bool Enabled { get; set; } = false;
    public string FilterType { get; set; } = "none";
    public List<string> BlockedDomains { get; set; } = [];
}
