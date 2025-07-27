# 🛠 Установка и настройка

## 📋 Системные требования

### Основные компоненты
| Компонент       | Версия       | Примечание                                  |
|-----------------|--------------|---------------------------------------------|
| .NET Runtime    | 8.0+         | Обязательно для запуска                    |
| MySQL Server    | 9.3+         | Требуется для хранения данных (также можно использовать SQLite)              |
| yt-dlp          | 2025.04.09+  | Должен быть установлен в системе (или лежать в корне проекта с исполняемым файлом) |
| gallery-dl      | 2025.04.09+  | Должен быть установлен в системе (или лежать в корне проекта с исполняемым файломю. Скачивается отдельно не входит в релиз) |

### Поддерживаемые ОС
- **Linux** (x64): Для разработки и использования я использовал Linux Mint и CachyOS. Поэтому, похожие дистрибутивы должны также работать, главное наличие основных компонентов в вашей системе
- **Windows** 10/11 (x64) - требуется ручная сборка, а также дополнительная проверка
- **macOS** (x64/ARM) - не проверялась

## 🚀 Быстрый старт для Linux

Перед началом работы с проектом необходимо установить необходимые инструменты. Выполните следующие команды если у вас их ещё нету:

### 1. Установка зависимостей для запуска из исходников

#### Для Debian/Ubuntu:
```bash
# Установка .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-8.0 git mysql-server libicu-dev

# Клонирование и настройка
git clone https://github.com/ZenonEl/TelegramMediaRelayBot.git
cd TelegramMediaRelayBot
# Скачивание бинарника gallery-dl (если хотите его использовать)
wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
chmod +x gallery-dl

# Запуск
dotnet run --project TelegramMediaRelayBot.csproj
```

#### Для Arch Linux:
```bash
# Установка зависимостей
sudo pacman -S dotnet-sdk git mariadb icu

# Клонирование и настройка
git clone https://github.com/ZenonEl/TelegramMediaRelayBot.git
cd TelegramMediaRelayBot
# Скачивание бинарника gallery-dl (если хотите его использовать)
wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
chmod +x gallery-dl

# Запуск
dotnet run --project TelegramMediaRelayBot.csproj
```

#### Для Fedora/RHEL:
```bash
# Установка .NET 8
sudo rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm
sudo dnf install -y dotnet-sdk-8.0 git mysql-server libicu

# Клонирование и настройка
git clone https://github.com/ZenonEl/TelegramMediaRelayBot.git
cd TelegramMediaRelayBot
# Скачивание бинарника gallery-dl (если хотите его использовать)
wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
chmod +x gallery-dl

# Запуск
dotnet run --project TelegramMediaRelayBot.csproj
```

#### 1.1 Запуск через исполняемый файл

1. Скачайте последний [релиз](https://github.com/ZenonEl/TelegramMediaRelayBot/releases/latest)
2. Распакуйте архив в удобное место
3. Создайте файл appsettings.json:
    - Используйте пример конфигурации из файла appsettings.json.example
4. Скачайте gallery-dl (если хотите его использовать):
    ```bash
    wget https://github.com/mikf/gallery-dl/releases/latest/download/gallery-dl.bin -O gallery-dl.bin
    chmod +x gallery-dl
    ```
5. Запустите исполняемый файл:
    **Перед запуском убедитесь, что выполнены все остальные шаги настройки**
    ```bash
    ./TelegramMediaRelayBot
    ```


---

### **2. Настройка MySQL/MariaDB**

#### **2.1 Создание базы данных и пользователя**

Выполните следующие команды в MySQL/MariaDB для создания базы данных и пользователя:

```sql 
-- Создание базы данных
CREATE DATABASE TelegramMediaRelayBot;

-- Создание пользователя и установка пароля
CREATE USER 'media_bot'@'localhost' IDENTIFIED BY 'StrongPassword123!';

-- Предоставление прав пользователю на базу данных
GRANT ALL PRIVILEGES ON TelegramMediaRelayBot.* TO 'media_bot'@'localhost';

-- Применение изменений
FLUSH PRIVILEGES;
```

---

#### **2.2 Настройка конфигурации**

После настройки MySQL/MariaDB, обновите конфигурационный файл `appsettings.json`. Убедитесь, что следующие параметры соответствуют вашей настройке MySQL/MariaDB:

```json
{
    "AppSettings": {
        "SqlConnectionString": "Server=localhost;Database=TelegramMediaRelayBot;User ID=media_bot;Password=StrongPassword123!;",
        "DatabaseType": "MySQL",
        "DatabaseName": "TelegramMediaRelayBot"
    }
}

# Также поддерживается использование SQLite
{
    "AppSettings": {
        "SqlConnectionString": "Data Source=sqlite.db",
        "DatabaseType": "SQLite",
        "DatabaseName": "TelegramMediaRelayBot"
    }
}
```

- **Server**: Адрес сервера MySQL (обычно `localhost`, если MySQL установлен на том же сервере или ПК).
- **Database**: Название базы данных (в данном случае `TelegramMediaRelayBot`).
- **User ID**: Имя пользователя (в данном случае `media_bot`).
- **Password**: Пароль пользователя (в данном случае `StrongPassword123!`).

---

### 3. Работа с конфигурацией
```bash
cp appsettings.json.example \
   appsettings.json

# Редактируем конфиг
nano ./appsettings.json
```
Пример конфигурации:
    - Если вам не нужен Tor или другой прокси то оставьте "Proxy" пустым ("") и в Tor.Enabled напишите false
    - В остальном же, кроме значений в блоке "AppSettings" больше можно ничего не менять.
    - Токен для "TelegramBotToken" можно получить только в официальном боте телеграм [BotFather](https://t.me/BotFather)
    - Для блока "AccessPolicy" имеется отдельное руководство.
    - AccessDeniedMessageContact поле в котором вы можете указать контакт для обратной связи, если планируете оставить бота закрытым для новых пользователей.

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
