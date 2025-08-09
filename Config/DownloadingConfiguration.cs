// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

namespace TelegramMediaRelayBot.Config;

public class DownloadingConfiguration
{
    // Telegram upload limit target (MB) to which files should be adapted before sending
    public int TargetUploadLimitMb { get; set; } = 50;

    // External source size cap (MB): if known remote size exceeds this value, skip download (preflight)
    public int ExternalDownloadMaxSizeMb { get; set; } = 0; // 0 = disabled

    public bool PreflightEnabled { get; set; } = true;

    public TooLargeHandling IfTooLarge { get; set; } = TooLargeHandling.Off;

    // For splitting large media into parts prior to upload
    public int TargetPartSizeMb { get; set; } = 48;

    public TranscodeProfileConfiguration? TranscodeProfile { get; set; } = new();
}

public enum TooLargeHandling
{
    Off = 0,
    Transcode = 1,
    Split = 2,
    TranscodeThenSplit = 3
}

public class TranscodeProfileConfiguration
{
    public string MaxResolution { get; set; } = "720p";
    public int VideoBitrateKbps { get; set; } = 2500;
    public int AudioBitrateKbps { get; set; } = 128;
    public string Preset { get; set; } = "fast";
}

