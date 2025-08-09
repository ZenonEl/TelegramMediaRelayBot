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
    "Defaults": {
      "Enabled": true,
      "Priority": 10,
      "MatchAllIfNoPatterns": true,
      "UrlPatterns": [],
      "ProxySettings": { "UseGlobalProxy": true, "CustomProxy": null, "RequireProxy": false }
    },
    "YtDlp": {
      "Type": "Executable",
      "Enabled": true,
      "Priority": 100,
      "Path": "yt-dlp",
      "CheckCommands": ["--dry-run", "--list-formats"],
      "DefaultArguments": ["--proxy", "{Proxy}", "--output", "{OutputPath}/video.%(ext)s"],
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
      "DefaultArguments": ["--proxy", "{Proxy}", "-d", "{OutputPath}", "-D", "{OutputPath}"]
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

### Блок `Downloaders.Defaults`
- Это секция с удобными значениями «по умолчанию», чтобы не дублировать их в каждом загрузчике.
- Текущая реализация не делает автоматического наследования на уровне кода. Поэтому, чтобы значение гарантированно применилось, задавайте его в узле конкретного загрузчика. `Defaults` служит как единый шаблон/референс для копирования и для будущего расширения.
  - Рекомендуемая дисциплина: поддерживайте одинаковые значения в `Defaults` и в нужных загрузчиках.

## Что можно менять без рестарта
- `Enabled`, `Priority`, `Path`, `AlternativePaths`, `CheckCommands`, `DefaultArguments`, `Timeout`, `MaxRetries`, `UrlPatterns`, `OutputPattern`, `ProgressPattern`, `ProxySettings`, `Defaults:MatchAllIfNoPatterns` — применяются без рестарта.

### Важно про hot‑reload downloader‑config
- Изменения в `downloader-config.json` НЕ логируются через общий монитор конфигурации (нет строк «Config changed [...]»).
- Эти изменения применяются «лениво»: при следующем запуске соответствующего загрузчика. Рестарт не нужен.
- Для списка `UrlPatterns` добавлена лёгкая проверка и обновление «на лету»: при первом обращении `CanHandle` после правки файла паттерны будут перечитаны. В лог прилетит Debug‑сообщение вида: `UrlPatterns reloaded for {Downloader}`.
- Секция `Downloaders.Defaults` служит шаблоном и не применяется автоматически как наследование. Продублируйте нужные поля в конкретном узле загрузчика.

## ENV‑переменные (перекрытие)
- Любой параметр можно переопределить через ENV, используя двойные подчёркивания как разделители:
  - `DOWNLOADERS__YTDLP__PROXYSETTINGS__REQUIREPROXY=true`
  - `DOWNLOADERS__GALLERYDL__ENABLED=false`
  - `DOWNLOADERS__DEFAULTS__MATCHALLIFNOPATTERNS=false`
  - Секреты (логин/пароль прокси) держите только в ENV.

## Логирование
- Мы больше не добавляем `-v/--verbose` в аргументы по умолчанию. Детальный вывод внешних тулов показывается в Debug и обрезается «хвостом».
- Изменения секции `Downloading` основного конфига логируются как `Config changed [Downloading]: ...`.
- Изменения в `downloader-config.json` (внешний файл) не логируются централизованно; ориентируйтесь на Debug‑сообщения загрузчиков и поведение при следующем запуске.

## Рекомендации
- Проверяйте корректность регулярных выражений для `UrlPatterns`.
- При спорных изменениях сначала тестируйте на одной ссылке.
- Если загрузчик требует обязательный прокси, установите `RequireProxy: true` и задайте `CustomProxy`.

