# 🛠 Installation and Configuration

## 📋 System Requirements

### Core Components
| Component       | Version       | Notes                                  |
|-----------------|--------------|---------------------------------------------|
| .NET Runtime    | 8.0+         | Required for running the bot                    |
| MySQL Server    | 8.0+         | Required for data storage              |
| yt-dlp          | 2024.04.09+  | Must be placed in the project root alongside the executable |

### Supported OS
- **Linux** (x64): Developed and tested on Linux Mint. Similar distributions should work as long as the core components are installed.
- **Windows** 10/11 (x64) - Requires manual build and additional testing.
- **macOS** (x64/ARM) - Not tested.

## 🚀 Quick Start for Linux

#### General Steps for Ubuntu/Debian

Before starting with the project, ensure the necessary tools are installed. Run the following commands if you don't already have them:
```bash
sudo apt update && sudo apt install -y \
    git \
    mysql-server \
    libicu-dev
```

### 1. Installing Dependencies for Running from Source
```bash
# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-8.0

# Clone the repository
git clone https://github.com/ZenonEl/TelegramMediaRelayBot.git
cd TelegramMediaRelayBot

# Download yt-dlp to the project root
wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O yt-dlp
chmod +x yt-dlp

# Run the project
# Ensure all other setup steps are completed before running
dotnet run --project TelegramMediaRelayBot.csproj
# Or
dotnet restore
dotnet run
```

#### 1.1 Running via Executable

1. Download the latest [release](https://github.com/ZenonEl/TelegramMediaRelayBot/releases/latest).
2. Extract the archive to a convenient location.
3. Create the `appsettings.json` file:
    - Use the example configuration from `appsettings.json.example`.
4. Download yt-dlp:
    ```bash
    wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O yt-dlp
    chmod +x yt-dlp
    ```
5. Run the executable:
    **Ensure all other setup steps are completed before running.**
    ```bash
    ./TelegramMediaRelayBot
    ```

---

### **2. Configuring MySQL**

#### **2.1 Creating the Database and User**

Run the following commands in MySQL to create the database and user:

```sql
-- Create the database
CREATE DATABASE TelegramMediaRelayBot;

-- Create the user and set the password
CREATE USER 'media_bot'@'localhost' IDENTIFIED BY 'StrongPassword123!';

-- Grant privileges to the user for the database
GRANT ALL PRIVILEGES ON TelegramMediaRelayBot.* TO 'media_bot'@'localhost';

-- Apply changes
FLUSH PRIVILEGES;
```

---

#### **2.2 Configuration Setup**

After setting up MySQL, update the configuration file `appsettings.json`. Ensure the following parameters match your MySQL setup:

```json
{
    "AppSettings": {
        "SqlConnectionString": "Server=localhost;Database=TelegramMediaRelayBot;User ID=media_bot;Password=StrongPassword123!;",
        "DatabaseName": "TelegramMediaRelayBot"
    }
}
```

- **Server**: MySQL server address (usually `localhost` if MySQL is installed on the same server or PC).
- **Database**: Database name (in this case, `TelegramMediaRelayBot`).
- **User ID**: Username (in this case, `media_bot`).
- **Password**: User password (in this case, `StrongPassword123!`).

---

### 3. Working with Configuration
```bash
cp appsettings.json.example \
   appsettings.json

# Edit the config
nano ./appsettings.json
```

Example configuration:
    - If you don't need Tor, leave "Proxy" empty ("") and set `Tor.Enabled` to `false`.
    - Apart from the values in the "AppSettings" block, you don't need to change anything else.
    - The token for "TelegramBotToken" can only be obtained from the official Telegram bot [BotFather](https://t.me/BotFather).
    - For the "AccessPolicy" block, refer to the dedicated guide.

```json
{
    "AppSettings": {
        "TelegramBotToken": "1234:abcd",
        "SqlConnectionString": "Server=localhost;Database=TelegramMediaRelayBot;User ID=media_bot;Password=StrongPassword123!;",
        "DatabaseName": "TelegramMediaRelayBot",
        "Language": "en-US",
        "Proxy": "socks5://127.0.0.1:9050",
        "UserUnMuteCheckInterval": 20
    },
    "Tor": {
        "Enabled": true,
        "TorControlPassword": "Password",
        "TorSocksHost": "127.0.0.1",
        "TorSocksPort": 9050,
        "TorControlPort": 9051,
        "TorChangingChainInterval": 5
    },
    "MessageDelaySettings": {
        "VideoGetDelay": 1000,
        "ContactSendDelay": 1000
    },
    "ConsoleOutputSettings": {
        "LogLevel": "Information",
        "ShowVideoDownloadProgress": false,
        "ShowVideoUploadProgress": false
    },
    "AccessPolicy": {
        "Enabled": true,

        "NewUsersPolicy": {
            "Enabled": true,

            "AllowNewUsers": true,
            "AllowRules": {
                "AllowAll": false,
                "WhitelistedReferrerIds": [],
                "BlacklistedReferrerIds": []
            }
        }
    }
}
```