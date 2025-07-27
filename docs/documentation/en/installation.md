# 🛠 Installation and Configuration

## 📋 System Requirements

### Core Components
| Component       | Version       | Notes                                  |
|-----------------|--------------|---------------------------------------------|
| .NET Runtime    | 8.0+         | Required for running the bot                    |
| MySQL Server    | 9.3+         | Need for storing data (also can use SQLite)              |
| yt-dlp          | 2025.04.09+  | Must be installed in the system (or placed in the root project with an executable file) |
| gallery-dl      | 2025.04.09+  | Must be installed in the system (or placed in the root project with an executable file). Downloaded separately does not enter the release |

### Supported OS
- **Linux** (x64): I used Linux Mint and CachyOS for development and use. Therefore, similar distributions should also work, the main thing is to have the basic components on your system
- **Windows** 10/11 (x64) - manual build required and additional verification
- **macOS** (x64/ARM) - not verified

## 🚀 Quick Start for Linux

Before you start working with the project, you need to install the necessary tools. Run the following commands if you don't have them already:

#### For Debian/Ubuntu:
```bash
# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-8.0 git mysql-server libicu-dev

# Clone and configure
git clone https://github.com/ZenonEl/TelegramMediaRelayBot. git
cd TelegramMediaRelayBot
# Download the gallery-dl binary (if you want to use it)
wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
chmod +x gallery-dl

# Run
dotnet run --project TelegramMediaRelayBot.csproj
```

#### For Arch Linux:
```bash
# Install dependencies
sudo pacman -S dotnet-sdk git mariadb icu

# Clone and customize
git clone https://github.com/ZenonEl/TelegramMediaRelayBot. git
cd TelegramMediaRelayBot
# Download the gallery-dl binary (if you want to use it)
wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
chmod +x gallery-dl

# Run
dotnet run --project TelegramMediaRelayBot.csproj
```

#### For Fedora/RHEL:
```bash
# Install . NET 8
sudo rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm
sudo dnf install -y dotnet-sdk-8.0 git mysql-server libicu

# Clone and configure
git clone https://github.com/ZenonEl/TelegramMediaRelayBot.git
cd TelegramMediaRelayBot
# Download the gallery-dl binary (if you want to use it)
wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
chmod +x gallery-dl

# Run
dotnet run --project TelegramMediaRelayBot.csproj
```

#### 1.1 Running via Executable

1. Download the latest [release](https://github.com/ZenonEl/TelegramMediaRelayBot/releases/latest).
2. Extract the archive to a convenient location.
3. Create the `appsettings.json` file:
    - Use the example configuration from `appsettings.json.example`.
4. Download gallery-dl (If you want to use it):
    ```bash
    wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
    chmod +x gallery-dl
    ```
5. Run the executable:
    **Ensure all other setup steps are completed before running.**
    ```bash
    ./TelegramMediaRelayBot
    ```

---

### **2. Configuring MySQL/MariaDB**

#### **2.1 Creating a Database and User**

Run the following commands in MySQL/MariaDB to create a database and user:

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

After configuring MySQL/MariaDB, update the `appsettings.json` configuration file. Make sure that the following parameters match your MySQL/MariaDB configuration:

```json
{
    "AppSettings": {
        "SqlConnectionString": "Server=localhost;Database=TelegramMediaRelayBot;User ID=media_bot;Password=StrongPassword123!;",
        "DatabaseType": "MySQL",
        "DatabaseName": "TelegramMediaRelayBot"
    }
}

# Also supported SQLite
{
    "AppSettings": {
        "SqlConnectionString": "Data Source=sqlite.db",
        "DatabaseType": "SQLite",
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
    - If you don't need Tor or other proxy, leave "Proxy" empty (""") and write false in Tor.Enabled
    - Apart from the values in the "AppSettings" block, you don't need to change anything else.
    - The token for "TelegramBotToken" can only be obtained from the official Telegram bot [BotFather](https://t.me/BotFather).
    - For the "AccessPolicy" block, refer to the dedicated guide.
    - AccessDeniedMessageContact field in which you can specify a contact for feedback if you plan to leave the bot closed to new users.

```json
{
    "AppSettings": {
        "TelegramBotToken": "1234:abcd",
        "SqlConnectionString": "Server=localhost;Database=DatabaseName;User ID=UserName;Password=UserPassword;",
        "DatabaseName": "TelegramMediaRelayBot",
        "DatabaseType": "MySql",
        "Language": "en-US",
        "Proxy": "socks5://127.0.0.1:9050",
        "UserUnMuteCheckInterval": 20,
        "UseGalleryDl": true,
        "AccessDeniedMessageContact": ""
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
        "Enabled": false,

        "NewUsersPolicy": {
            "Enabled": false,
            "ShowAccessDeniedMessage": false,

            "AllowNewUsers": true,
            "AllowRules": {
                "AllowAll": true,
                "WhitelistedReferrerIds": [],
                "BlacklistedReferrerIds": []
            }
        }
    }
}
```
