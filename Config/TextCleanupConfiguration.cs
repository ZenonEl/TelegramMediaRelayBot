// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

namespace TelegramMediaRelayBot.Config;

public class TextCleanupConfiguration
{
    public bool Enabled { get; set; } = true;
    public List<TextCleanupRule> Rules { get; set; } = new();
}

public class TextCleanupRule
{
    public List<string> Domains { get; set; } = new();
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;
}

