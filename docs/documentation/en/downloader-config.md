# ⚙️ Downloader configuration (downloader-config.json)

This file controls external downloaders (yt-dlp, gallery-dl, etc.). It is loaded in addition to the main app configuration and is reloaded on change (no restart needed).

## Location
- The path is set in `appsettings.json`: `AppSettings:DownloaderSettings:ConfigFilePath`
- Example: `"AppSettings": { "DownloaderSettings": { "ConfigFilePath": "./downloader-config.json" } }`
- Changing the path requires restart. Editing the file content is applied without restart.

## Sources priority
1) Environment variables (ENV) — override lower sources
2) `appsettings.json`
3) `appsettings.example.json`
4) `downloader-config.json` — extends configuration (`Downloaders` and `GlobalSettings`)

> Note: downloader-config.json is not intended for bot token, DB config, etc.; it describes downloader behavior only.

## Structure
Minimal example:
```json
{
  "Downloaders": {
    "YtDlp": {
      "Type": "Executable",
      "Enabled": true,
      "Priority": 100,
      "Path": "yt-dlp",
      "CheckCommands": ["--dry-run", "--list-formats"],
      "DefaultArguments": [
        "--proxy", "{Proxy}",
        "-v",
        "-f", "best[filesize<50M]/worst[filesize<50M]/best",
        "--output", "{OutputPath}/video.%(ext)s"
      ],
      "ProxySettings": {
        "UseGlobalProxy": true,
        "CustomProxy": null,
        "RequireProxy": false,
        "SiteSpecificProxies": {
          "vk.com": null,
          "pinterest.com": null
        }
      },
      "Timeout": "00:10:00",
      "MaxRetries": 3,
      "UrlPatterns": [
        "youtube\\.com/watch\\?v=",
        "youtu\\.be/[a-zA-Z0-9_-]+"
      ],
      "OutputPattern": "\\[download\\] Destination: (.+)",
      "ProgressPattern": "\\[download\\]"
    },
    "GalleryDl": {
      "Type": "Executable",
      "Enabled": true,
      "Priority": 90,
      "Path": "gallery-dl",
      "AlternativePaths": ["gallery-dl.bin"],
      "SupportedMediaTypes": "Image",
      "CheckCommands": ["--list-urls", "--print filename"],
      "DefaultArguments": ["--proxy", "{Proxy}", "-d", "{OutputPath}", "-D", "{OutputPath}", "--verbose"],
      "ProxySettings": {
        "UseGlobalProxy": true,
        "CustomProxy": null,
        "RequireProxy": false,
        "SiteSpecificProxies": {
          "vk.com": null,
          "pinterest.com": null
        }
      },
      "Timeout": "00:05:00",
      "MaxRetries": 2,
      "UrlPatterns": ["pinterest\\.com/pin/"]
    }
  },
  "GlobalSettings": {
    "DefaultTimeout": "00:10:00"
  }
}
```

## How it works in code
- Downloader parameters are read via `IConfiguration` on each usage → changes apply to subsequent runs.
- Placeholders:
  - `{Proxy}` — replaced with the current proxy (global from `AppSettings:Proxy` or site-specific from `ProxySettings:SiteSpecificProxies`).
  - `{OutputPath}` — temporary download directory.

## What can be changed without restart
- `Enabled`, `Priority`, `Path`, `AlternativePaths`, `CheckCommands`, `DefaultArguments`, `Timeout`, `MaxRetries`, `UrlPatterns`, `OutputPattern`, `ProgressPattern`, `ProxySettings`.

## Recommendations
- Validate `UrlPatterns` regexes.
- For risky changes, test on a single URL first.
- If a downloader requires a proxy, set `RequireProxy: true` and provide `CustomProxy`.

