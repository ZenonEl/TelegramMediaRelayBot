# 🛠 Установка и настройка

## 📋 Системные требования

### Основные компоненты
| Компонент       | Версия       | Примечание                                  |
|-----------------|--------------|---------------------------------------------|
| .NET Runtime    | 8.0+         | Обязательно для запуска                    |
| MySQL Server    | 8.0+         | Требуется для хранения данных              |
| yt-dlp          | 2024.04.09+  | Должен быть рядом (в корне проекта) с исполняемым файлом |

### Поддерживаемые ОС
- **Linux** (x64): Для разработки и использования я использовал Linux Mint. Поэтому, похожие дистрибутивы должны также работать, главное наличие основных компонентов в вашей системе
- **Windows** 10/11 (x64) - требуется ручная сборка, а также дополнительная проверка
- **macOS** (x64/ARM) - не проверялась

## 🚀 Быстрый старт для Linux

#### Общие действия для Ubuntu/Debian

Перед началом работы с проектом необходимо установить необходимые инструменты. Выполните следующие команды если у вас их ещё нету:
```bash
sudo apt update && sudo apt install -y \
    git \
    mysql-server \
    libicu-dev
```

### 1. Установка зависимостей для запуска из исходников
```bash
# Установка .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-8.0

# Клонирование репозитория
git clone https://github.com/ZenonEl/TelegramMediaRelayBot.git
cd TelegramMediaRelayBot

# Скачивание yt-dlp в корень проекта
wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O yt-dlp
chmod +x yt-dlp

# Запуск проекта
# Перед запуском убедитесь, что выполнены все остальные шаги настройки
dotnet run --project TelegramMediaRelayBot.csproj
# Или
dotnet restore
dotnet run
```
#### 1.1 Запуск через исполняемый файл

1. Скачайте последний [релиз](https://github.com/ZenonEl/TelegramMediaRelayBot/releases/latest)
2. Распакуйте архив в удобное место 
3. Создайте файл appsettings.json:
    - Используйте пример конфигурации из файла appsettings.json.example
4. Скачайте yt-dlp:
    ```bash
    wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O yt-dlp
    chmod +x yt-dlp
    ```
5. Запустите исполняемый файл:
    **Перед запуском убедитесь, что выполнены все остальные шаги настройки**
    ```bash
    ./TelegramMediaRelayBot
    ```


---

### **2. Настройка MySQL**

#### **2.1 Создание базы данных и пользователя**

Выполните следующие команды в MySQL для создания базы данных и пользователя:

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

После настройки MySQL, обновите конфигурационный файл `appsettings.json`. Убедитесь, что следующие параметры соответствуют вашей настройке MySQL:

```json
{
    "AppSettings": {
        "SqlConnectionString": "Server=localhost;Database=TelegramMediaRelayBot;User ID=media_bot;Password=StrongPassword123!;",
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
    - Если вам не нужен Tor то оставьте "Proxy" пустым ("") и в Tor.Enabled напишите false
    - В остальном же, кроме значений в блоке "AppSettings" больше можно ничего не менять.
    - Токен для "TelegramBotToken" можно получить только в официальном боте телеграм [BotFather](https://t.me/BotFather)
    - Для блока "AccessPolicy" имеется отдельное руководство.
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