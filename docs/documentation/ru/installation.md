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

#### 3.1 Порядок источников конфигурации и переменные окружения (ENV)

Конфигурация загружается в следующем порядке (выше — приоритетнее, перезаписывает нижележащие значения):
1) Переменные окружения (ENV)
2) `appsettings.json`
3) `appsettings.example.json`

Для вложенных ключей в ENV используется разделитель `__` (два подчёркивания).

Примеры ключей ENV:
- `AppSettings__TelegramBotToken`
- `AppSettings__SqlConnectionString`
- `AppSettings__DatabaseType`
- `AppSettings__DownloaderSettings__ConfigFilePath`
- `Tor__Enabled`
- `Tor__TorSocksHost`
- `ConsoleOutputSettings__LogLevel`

Примеры для Linux:
- bash/zsh:
  ```bash
  export AppSettings__TelegramBotToken="1234:abcd"
  export ConsoleOutputSettings__LogLevel="Debug"
  export Tor__Enabled="true"
  ```
- fish:
  ```fish
  set -x AppSettings__TelegramBotToken "1234:abcd"
  set -x ConsoleOutputSettings__LogLevel "Debug"
  set -x Tor__Enabled "true"
  ```
Для разового запуска можно использовать:
```bash
AppSettings__TelegramBotToken="1234:abcd" Tor__Enabled=true dotnet run --project TelegramMediaRelayBot.csproj
```

> Примечание: значения из ENV имеют приоритет над `appsettings.json`.
> Важно: переменные окружения не обновляются «на лету». Изменения ENV во время работы процесса не будут применены автоматически — перезапустите бот.

#### 3.2 Обновление параметров без рестарта

Файлы `appsettings.json` и `downloader-config.json` подгружаются с `reloadOnChange: true`. При изменении безопасных параметров бот применяет их автоматически и пишет об этом в логах. Переменные окружения в «обновлении без рестарта» не участвуют.

Применяются на лету:
- `ConsoleOutputSettings:LogLevel` — уровень логирования
- `Tor:Enabled`, `Tor:TorChangingChainInterval` — таймеры Tor
- `Tor:TorSocksHost`, `Tor:TorSocksPort`, `Tor:TorControlPort`, `Tor:TorControlPassword` — используются при следующей смене цепочки
- `MessageDelaySettings:UserUnMuteCheckInterval` — период проверки размьюта
- `MessageDelaySettings:ContactSendDelay` — задержка отправки сообщений/контактов
- `AppSettings:Proxy` — глобальный прокси для загрузчиков
- `AccessPolicy` — политика доступа для новых пользователей
- Параметры загрузчиков из `downloader-config.json` (`Downloaders:*`) — включение/приоритет/аргументы/таймауты/паттерны

Требует рестарта:
- `AppSettings:TelegramBotToken`
- `AppSettings:DatabaseType`, `AppSettings:SqlConnectionString`, `AppSettings:DatabaseName`
- Смена пути к файлу `AppSettings:DownloaderSettings:ConfigFilePath` (содержимое самого файла — горячее)

Логи конфигурационных изменений:
- При каждом применении изменений пишется строка вида: `Applied hot config [...]` и/или `Config changed [...]`.

#### 3.3 Конфиг загрузчиков (downloader-config.json)

- Путь задаётся ключом `AppSettings:DownloaderSettings:ConfigFilePath` (например: `./downloader-config.json`).
- Содержимое файла подхватывается на лету (`reloadOnChange: true`).
- Смена самого пути требует рестарта.

Подробнее про структуру и примеры: см. раздел «[Конфигурация загрузчиков](downloader-config.md)».

#### 3.4 Рекомендации по секретам и не-горячим параметрам

- Храните в ENV значения, которые не обновляются без рестарта и/или являются секретами:
  - `AppSettings:TelegramBotToken`
  - `AppSettings:SqlConnectionString`
  - `AppSettings:DatabaseType`
  - `AppSettings:DownloaderSettings:ConfigFilePath`
  - Любые будущие секреты (пароли, API-ключи и т.д.)

> Обоснование: ENV имеет более высокий приоритет и не меняется «на лету», что снижает риск случайного изменения в runtime и упрощает безопасное управление секретами.

