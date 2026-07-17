<p align="center">
 <img src="Logo.jpg" width="512" height="384" alt="Logo">
</p>

<div align="center">

![License](https://img.shields.io/badge/License-AGPL--3.0-blue)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple)
![Telegram Bot API](https://img.shields.io/badge/Telegram%20Bot%20API-22.10.2-green)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/ZenonEl/TelegramMediaRelayBot)

</div>

<div align="center">

[README на русском языке](docs/README_RU.md)

</div>

**TelegramMediaRelayBot** is a self-hosted Telegram bot that automates downloading media from 1000+ platforms (via [yt-dlp](https://github.com/yt-dlp/yt-dlp/tree/master) and gallery-dl) and forwards it to your contacts. It eliminates manual downloads, simplifies media sharing across platforms users don't have, and gives full control over privacy by running entirely on your infrastructure.

---

## Key Features

- **Multi-platform downloads**: yt-dlp for video, gallery-dl for galleries — tried as a fallback chain. Full list of [supported sites](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md).
- **Contact forwarding**: managed contact list inside the bot — downloaded media is forwarded automatically, with per-contact and per-link privacy rules.
- **Large files up to 2 GB**: optional local Telegram Bot API server; media is streamed from disk instead of buffered in memory.
- **Resumable queue**: pending downloads are persisted, so a crash or reboot never loses a request.
- **Flexible proxying**: HTTP / SOCKS5 / Tor, with per-site rules — route some domains through a proxy and others directly.
- **Self-hosted**: full control over your data and privacy. No third-party services.

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Runtime** | .NET 10 |
| **Bot framework** | Telegram.Bot 22.10.2 |
| **Language** | C# 14 |
| **Database** | SQLite (Dapper + FluentMigrator) |
| **Media engines** | yt-dlp, gallery-dl |
| **Container** | Docker / docker-compose |
| **License** | AGPL-3.0 |

## Architecture Highlights

- **Self-healing long polling**: the update loop runs as a hosted service that logs and skips a failing update and restarts on error, so the bot can't silently stop responding.
- **Modular download backends**: yt-dlp and gallery-dl sit behind one `IMediaDownloader` interface and run as an ordered fallback chain — adding a backend is one class.
- **Per-host proxy routing**: named proxies resolved per download host from typed, startup-validated configuration.
- **2 GB delivery path**: with a local Bot API server the bot sends media by file path off a shared volume — no HTTP upload, no memory blow-up.
- **Persistent download queue** with bounded concurrency; jobs are resumed after a restart.
- **Repository-pattern data layer** over SQLite with schema migrations.

## Documentation

Full documentation is available on the project site:
**[zenonel.github.io/TelegramMediaRelayBot-Site](https://zenonel.github.io/TelegramMediaRelayBot-Site)**

Or in [`docs/documentation/en/`](docs/documentation/en/index.md) inside the repo. Covers setup, installation, configuration, and usage.

## Roadmap

Active development plans are tracked on the latest [release page](https://github.com/ZenonEl/TelegramMediaRelayBot/releases). Strategic direction:

- **Plugin system** with monetization-ready plugins for commercial use cases.
- *(Under review)* **Mobile-side runtime** for users who can't run servers.
- *(Under consideration)* Text formatting support, in-bot administrative functions.
- General improvements and fixes.

[CHANGELOG.md](CHANGELOG.md) — full history of changes.

## Logging

The bot logs all actions to console; file logging is on the roadmap. Logging level is configurable via the settings file.

## Origin

The project started from my girlfriend's complaints about manually downloading TikTok videos for me — I don't use the platform myself. What began as a one-off automation evolved into a full media-relay ecosystem: multi-user, multi-platform, plugin-extensible. The mission stayed simple — give people a self-hosted way to share content across platforms without surrendering their data to any single service.

## License

This project is licensed under **AGPL-3.0**. See [LICENSE](LICENSE) for details.

## Feedback

If you have questions, suggestions, or found a bug — please open an [issue](https://github.com/ZenonEl/TelegramMediaRelayBot/issues).

## Contributing

The project is currently not accepting external contributions, but this may change in future. Stay tuned.

---

**Copyright (C) 2024-2026 ZenonEl**

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
