# 📋 **COMPREHENSIVE IMPROVEMENT ROADMAP**
*TelegramMediaRelayBot Development Plan*

---

## 🔥 **КРИТИЧЕСКИЕ ПРИОРИТЕТЫ (Must Do First)**

### **Архитектурные Исправления**
- [x] **Рефакторинг Config класса** — заменить статический Config на IOptions<T> pattern
- [x] **Убрать статические состояния** — введён `IUserStateManager` вместо static Dictionary
- [x] **Исправить Service Locator антипаттерн** — убраны `BuildServiceProvider()` из регистрации, всё через DI
 - [x] **Переход DB к асинхронной модели** — репозитории переведены на async/await (остаток: единичные короткие sync Execute в write-методах — опционально)
- [x] **Добавить Unit of Work паттерн** для транзакций БД (Contacts, Groups, Privacy, DefaultActions — MySQL/SQLite)
- [x] **Рефакторинг больших методов** — разбиение `UpdateHandler` и `SendMediaToTelegram`
- [x] **Горячая перезагрузка конфигурации** (безопасные настройки)
  - [x] Перевод потребителей на `IOptionsMonitor<T>` (Tor, задержки, AccessPolicy, Proxy)
  - [x] Использование `OnChange` для Safe-настроек (delays, proxy, Tor) + логирование изменений
  - [x] Горячая смена `ConsoleOutputSettings:LogLevel` через `LoggingLevelSwitch`
  - [x] Исключения (рестарт): TelegramBotToken, тип БД/connection string (документация)

### **Базовое Тестирование**
- [x] **Unit тесты для ключевой функциональности** (MediaDownloader, Factory, Repositories)
- [x] **Integration тесты для БД** слоя (SQLite in-memory: миграции/схемы + CRUD)
- [x] **Тесты для критических business сценариев** (fallback при скачивании)

---

## ⚡ **ВЫСОКИЙ ПРИОРИТЕТ (Current Roadmap + Critical)**

### **Из Текущего Roadmap**
- [x] **Завершить Modular Downloader Architecture**
  - [x] Добавить валидацию конфигурации загрузчиков (минимальная, базовая)
  - [x] Улучшить логирование выбора загрузчика и применения параметров
- [x] **Intelligent Message Merging**
  - [x] Анализ предыдущих сообщений пользователя (pending‑подпись по `statusMessageId`)
  - [x] Объединение текста из разных сообщений (подхват "следующего текста" в окне)
  - [x] Временные окна для объединения (окно = задержка дефолт‑действия; дефолт 5 сек)
- [x] **Download Cancellation UI**
  - [x] UI для отмены загрузки (кнопка cancel_download)
  - [x] Уведомления об отмене (редактирование статус‑сообщения: "❎ Отменено пользователем")
  - [x] Очистка временных файлов при отмене (удаление temp‑директории в загрузчиках в блоке finally)

### **Дополнительные Критические Улучшения**
- [ ] **Валидация входных данных** через FluentValidation
- [x] **Улучшить Dependency Injection lifecycle** — репозитории переведены на Scoped
- [ ] **Rate Limiting** для защиты от злоупотреблений
- [x] **Санитизация URL'ов** для безопасности (выделение первой ссылки, игнор префикса)
- [x] **Санитизация подписи** (удаление HTML, сохранение Markdown, обрезка до лимита TG)

---

## 🎯 **СРЕДНИЙ ПРИОРИТЕТ (Important Features)**

### **Из Текущего Roadmap**
- [ ] **Smart Text Cleanup and Formatting**
  - [ ] Регулярные выражения для очистки "мусорного" текста
  - [ ] Конфигурируемые правила очистки
  - [ ] Опции размещения очищенного текста
- [ ] **Improve Message Formatting** (завершить 70%)
  - [ ] Более информативные caption'ы
  - [ ] Форматирование текста с HTML/Markdown
  - [ ] Метаданные о источнике (platform, duration, size)
- [x] **Завершить Detailed Downloader Parameter Configuration**
  - [x] Валидация конфигурации загрузчиков

### **Архитектурные Улучшения**
- [ ] **Добавить CQRS с MediatR** для команд и запросов
- [ ] **Result Pattern** вместо исключений для business logic
- [ ] **Кэширование** для часто используемых данных (IMemoryCache)
- [ ] **Retry механизм** с Polly для внешних вызовов
- [ ] **Улучшения старых мест в коде** рефакторинг старых код решений на более качественное решение
- [x] **Structured Logging** — улучшено логирование конфиг-изменений

---

## 📝 **НИЗКИЙ ПРИОРИТЕТ (Nice to Have)**

### **Из Текущего Roadmap**
- [ ] **Downloader Authorization Support**
  - [ ] Хранение credentials в конфигурации
  - [ ] Передача авторизационных данных в загрузчики
  - [ ] Безопасное хранение паролей/токенов (Azure KeyVault/HashiCorp Vault)
- [ ] **Built-in Database Backup**
  - [ ] Команды для создания бэкапов
  - [ ] Планировщик автоматических бэкапов
  - [ ] Восстановление из бэкапов
- [ ] **Add "Help" Button**
  - [ ] Кнопка Help в главном меню
  - [ ] Ссылки на документацию
  - [ ] Контекстная помощь

### **Дополнительные Улучшения**
- [ ] **Metrics и Monitoring** (Application Insights/Prometheus)
- [ ] **Health Checks** для внешних зависимостей
- [ ] **Circuit Breaker Pattern** для внешних API
- [ ] **Event Sourcing** для аудита действий пользователей
- [ ] **Background Jobs** с Hangfire для отложенных задач
- [ ] **API Documentation** с Swagger/OpenAPI
- [ ] **Docker Containerization** с multi-stage builds
- [ ] **CI/CD Pipeline** с GitHub Actions
- [ ] **Load Testing** с NBomber
- [ ] **Security Scanning** статического кода
- [ ] **Logger** добавить логирование в файл

---

## 🔧 **ТЕХНИЧЕСКИЙ ДОЛГ И РЕФАКТОРИНГ**

### **Code Quality**
- [ ] **Убрать code smells**:
  - [ ] Большие классы (разбить на меньшие)
  - [ ] Дублирование кода
  - [ ] Магические числа в константы
  - [ ] Длинные списки параметров
  - [ ] Вынести текст из кода в переводы
- [ ] **Naming Conventions** - унифицировать именование
- [ ] **XML Documentation** для публичных API

### **Performance**
- [ ] **Async/await best practices** - ConfigureAwait(false)
- [ ] **Memory optimization** - использование Span<T>, ArrayPool
- [ ] **Database query optimization** - анализ и оптимизация запросов
- [ ] **Connection pooling** настройка для БД

### **Security**
- [ ] **Input sanitization** для всех пользовательских данных
- [ ] **SQL injection protection** (уже есть через Dapper, но проверить)
- [ ] **XSS protection** для HTML контента
- [ ] **Secrets management** - убрать secrets из кода
- [ ] **HTTPS enforcement** для всех внешних вызовов

---

## 📊 **МЕТРИКИ И МОНИТОРИНГ**

- [ ] **Application Metrics**:
  - [ ] Количество обработанных сообщений
  - [ ] Success rate загрузок по загрузчикам
  - [ ] Время отклика системы
  - [ ] Использование памяти и CPU
- [ ] **Business Metrics**:
  - [ ] Активные пользователи
  - [ ] Популярные платформы
  - [ ] Размер загружаемых файлов
- [ ] **Error Tracking** с Sentry или аналогом
- [ ] **Performance Profiling** с dotTrace

---

## 🧪 **РАСШИРЕННОЕ ТЕСТИРОВАНИЕ**

- [ ] **Unit Tests**:
  - [ ] Domain models
  - [ ] Business logic
  - [ ] Utilities и helpers
- [ ] **Integration Tests**:
  - [ ] Database repositories
  - [ ] External API calls
  - [ ] End-to-end workflows
- [ ] **Performance Tests**:
  - [ ] Load testing
  - [ ] Stress testing
  - [ ] Memory leak detection
- [ ] **Security Tests**:
  - [ ] Penetration testing
  - [ ] Vulnerability scanning
  - [ ] Dependency security audit

---

## 🚀 **DEPLOYMENT И DEVOPS**

- [ ] **Infrastructure as Code**:
  - [ ] Docker compose для local development
  - [ ] Kubernetes manifests для production
  - [ ] Terraform/ARM templates для cloud resources
- [ ] **CI/CD Pipeline**:
  - [ ] Automated testing
  - [ ] Code quality gates
  - [ ] Automated deployment
  - [ ] Blue-green deployment strategy
- [ ] **Monitoring и Alerting**:
  - [ ] Application monitoring
  - [ ] Infrastructure monitoring  
  - [ ] Log aggregation (ELK stack)
  - [ ] Alert rules и notifications

---

## 📈 **ПЛАН ВЫПОЛНЕНИЯ**

### **Sprint 1 (2-3 недели) - Критические исправления**
**Цель: Исправить архитектурные проблемы**
- [x] Config рефакторинг на IOptions<T>
- [x] Убрать статические состояния (IUserStateManager)
 - [x] Переход к async/await в DB layer
- [x] Базовые unit тесты для core functionality
- [x] Исправить Service Locator антипаттерн
- [x] Горячая перезагрузка конфигурации (IOptionsMonitor<T> + OnChange)

### **Sprint 2 (2-3 недели) - Core Features**
**Цель: Реализовать ключевую функциональность**
- [ ] Intelligent Message Merging
- [ ] Download Cancellation UI
- [ ] Validation и Rate Limiting
- [x] Integration тесты для DB layer
- [ ] Завершить Modular Downloader Architecture

### **Sprint 3 (2-3 недели) - Quality Improvements**
**Цель: Улучшить качество и UX**
- [ ] Smart Text Cleanup and Formatting
- [ ] Improve Message Formatting (завершить)
- [ ] Parameter Configuration (завершить)
- [ ] Performance optimization (async best practices)
- [x] Unit of Work pattern

### **Sprint 4 (2-3 недели) - Advanced Architecture**
**Цель: Современные архитектурные паттерны**
- [ ] CQRS с MediatR
- [ ] Result Pattern
- [ ] Кэширование (IMemoryCache)
- [ ] Retry механизм (Polly)
- [ ] Structured Logging improvements

### **Sprint 5+ - Advanced Features**
**Цель: Дополнительная функциональность**
- [ ] Authorization support для загрузчиков
- [ ] Database Backup functionality
- [ ] Monitoring и Metrics
- [ ] Security hardening
- [ ] Help Button и Documentation

---

## 📊 **ТЕКУЩИЙ СТАТУС ROADMAP**

### **Завершено ✅**
- [x] Modular Downloader Architecture (100%)
- [x] Detailed Downloader Parameter Configuration (100%)
- [x] Download Cancellation (CancellationToken support - 20%)
- [x] Message Formatting (basic implementation - 30%)
- [x] Config Refactor to DI + Options
- [x] Remove Static State (IUserStateManager)
- [x] DI Hygiene (Scoped repositories, no Service Locator)
- [x] Hot Config (Tor, Delays, Proxy, AccessPolicy, LogLevel) + change logging
  - [x] Downloader-config: изменения применяются без рестарта, `UrlPatterns` обновляются «на лету», но без централизованного логирования
- [x] Async DB Model (core paths)
- [x] Unit of Work across write repositories (Contacts, Groups, Privacy, DefaultActions)
- [x] Basic Unit/Integration/Business tests

### **В Процессе ⚠️**
 - [x] Async DB Model — core части переведены на async/await (остались единичные короткие sync Execute в write-методах — опционально)
- [ ] Message Merging (0% - высокий приоритет)
- [ ] Text Cleanup (0%)

### **Не Начато ❌**
- [ ] Authorization Support
- [ ] Database Backup
- [ ] Help Button Integration

---

## 🎯 **МЕТРИКИ УСПЕХА**

### **Архитектурные Метрики**
- [ ] **Code Coverage** > 80% для unit тестов
- [ ] **Cyclomatic Complexity** < 10 для всех методов
- [ ] **Maintainability Index** > 70
- [ ] **Zero static dependencies** в core business logic

### **Performance Метрики**
- [ ] **Response Time** < 200ms для основных операций
- [ ] **Memory Usage** стабильное (без утечек)
- [ ] **Download Success Rate** > 95%
- [ ] **Database Query Time** < 100ms

### **Quality Метрики**
- [ ] **Zero Critical** security vulnerabilities
- [ ] **Code Duplication** < 5%
- [ ] **Documentation Coverage** > 90%

---

*Последнее обновление: 2025-01-07*
*Следующий review: Sprint 1 completion*
