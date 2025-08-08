# ⚙️ Конфигурация загрузчиков (downloader-config.json)

Этот файл управляет параметрами внешних загрузчиков (yt-dlp, gallery-dl и т.д.). Он подключается дополнительно к основному конфигу приложения и подхватывается без рестарта (reloadOnChange: true).

## Где лежит файл
- Путь задаётся в `appsettings.json` ключом: `AppSettings:DownloaderSettings:ConfigFilePath`
- Пример: `"AppSettings": { "DownloaderSettings": { "ConfigFilePath": "./downloader-config.json" } }`
- Смена пути требует рестарта приложения. Изменение содержимого файла — применяется без рестарта.

## Приоритет источников
1) Переменные окружения (ENV) — перезаписывают значения ниже
2) `appsettings.json`
3) `appsettings.example.json`
4) `downloader-config.json` — дополняет конфигурацию (раздел `Downloaders` и `GlobalSettings`)

> Примечание: downloader-config.json не предназначен для задания токена бота, БД и т.п.; он описывает только загрузчики.

## Структура файла
Минимальный пример:
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

## Как это работает в коде
- Параметры загрузчиков читаются при каждом использовании через `IConfiguration` → изменения применяются для следующих запусков команд.
- Плейсхолдеры:
  - `{Proxy}` — будет заменён на актуальный прокси (глобальный из `AppSettings:Proxy` или сайт-специфичный из `ProxySettings:SiteSpecificProxies`).
  - `{OutputPath}` — временная директория загрузки.

## Что можно менять без рестарта
- `Enabled`, `Priority`, `Path`, `AlternativePaths`, `CheckCommands`, `DefaultArguments`, `Timeout`, `MaxRetries`, `UrlPatterns`, `OutputPattern`, `ProgressPattern`, `ProxySettings` — все применяются для следующих загрузок без рестарта.

## Рекомендации
- Проверяйте корректность регулярных выражений для `UrlPatterns`.
- При спорных изменениях сначала тестируйте на одной ссылке.
- Если загрузчик требует обязательный прокси, установите `RequireProxy: true` и задайте `CustomProxy`.

