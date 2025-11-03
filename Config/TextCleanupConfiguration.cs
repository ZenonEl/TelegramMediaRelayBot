// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

namespace TelegramMediaRelayBot.Config;

public class TextCleanupConfig
{
    public List<string> Patterns { get; set; } = new();
    public string? PatternsFile { get; set; }
}
