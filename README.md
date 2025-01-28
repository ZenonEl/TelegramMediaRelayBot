<p align="center">
 <img src="Logo.jpg" width="512" height="384" alt="Logo">
</p>

<div align="center"> 
 
![License](https://img.shields.io/badge/License-AGPL--3.0-blue)
![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)
![Telegram Bot API](https://img.shields.io/badge/Telegram%20Bot%20API-22.1.3-green)
 
</div>

<div align="center">

[README на русском языке](docs/README_RU.md)

</div>

**TelegramMediaRelayBot** is a self-hosted Telegram bot that allows you to automatically download and forward videos from multiple [platforms](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md) (thanks to [yt-dlp](https://github.com/yt-dlp/yt-dlp/tree/master)) to you and your contacts. The bot simplifies the process of sharing media files, eliminating the need to manually download and send videos to those who do not use a particular platform.


#### Mini-Story of the Project Idea
The idea for this project originated from my girlfriend's complaints about having to manually download videos from TikTok for my convenience, as I do not use the platform myself. This led me to the idea of creating a bot that would automatically download videos and send them. However, over time, I became so engrossed in the project that I decided to scale it up.

Now, it does not just download videos and send them to a single user; it has evolved into something much greater—a constantly developing and growing mini-ecosystem that every user can recreate and invite the people they need into. This simplifies content sharing between users in various situations.

Thus, the project has transformed into a multifunctional tool for content exchange that can adapt to the needs of each user.

---


## Key Features

- **Video Downloading**: Support for multiple platforms via **yt-dlp** (possibly adding **gallery-dl** in the future).
- **Contact Forwarding**: Ability to add contacts within the bot to whom downloaded videos will be automatically forwarded.
- **Proxy**: Support for proxies (including Tor) for downloading videos.

## Project Details

For a more detailed understanding of the functionality and capabilities of our project, you can refer to the comprehensive documentation. It is available on our website at [this link](https://zenonel.github.io/TelegramMediaRelayBot-Site) or in the [docs](docs/documentation/en) folder located in the root directory of the project. In the documentation, you will find information not only about the features but also about the setup process, installation, and usage of our solution. We recommend reviewing it to make the most of all the functions our project offers.

---

## Changelog

The full history of changes can be found in [CHANGELOG.md ](CHANGELOG.md ).

## Future Plans
- (Under consideration) Adding support for **gallery-dl** to download media from even more platforms.
- More detailed contact management (~~deletion~~, editing, etc.).
- ~~Creating and managing contact groups within the bot~~.
- (Under consideration) Support for text formatting.
- Ability to recreate the personal link within the bot (with the option to delete all contacts or keep them).
- ~~Ability to enable a filter for accessing the bot (for example, you can only start using the bot by using someone's referral link).~~
- (Under consideration) Administrative functions for managing the bot within itself.
- ~~Creating a ready-to-use executable file.~~
- And various other improvements and fixes ✨

## Logging
The bot logs all actions to the console. In the future, logging to a file is planned. Logs can be configured in the settings file.



## License
The project is distributed under the **AGPL-3.0** license. Details can be found in the [LICENSE](LICENSE) file.



## Feedback
If you have questions, suggestions, or find a bug, please create an [issue](hhttps://github.com/ZenonEl/TelegramMediaRelayBot/issues) in the repository.



## Contributing
The project is not currently accepting contributions, but this may change in the future. Stay tuned for updates!



## Copyright (C) 2024-2025 ZenonEl

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.