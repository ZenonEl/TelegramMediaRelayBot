using Microsoft.Playwright;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TikTokMediaRelayBot
{
    public class VideoGet
    {
        public static async Task<string?> GetDownloadLink(string videoUrl, List<ElementAction> elementsPath)
        {
            using (var playwright = await Playwright.CreateAsync().ConfigureAwait(false))
            {
                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    Proxy = new Proxy
                    {
                        Server = "socks5://localhost:9150"
                    }
                };

                var browser = await playwright.Firefox.LaunchAsync(launchOptions);
                var page = await browser.NewPageAsync();

                Log.Debug("Браузер инициализирован. Открытие страницы...");
                await page.GotoAsync("https://tikvideo.app/ru");

                foreach (var action in elementsPath)
                {
                    Log.Debug($"Выполняется действие: {action.Action}, элемент: {action.Type}='{action.Value}'");
                    await PerformAction(page, action, videoUrl);
                }

                // Retrieve the final download link
                Log.Debug("Поиск финального элемента для получения ссылки...");
                var finalDownloadLinkElement = await page.QuerySelectorAsync($"#{Config.finalDownloadButtonClass}");
                if (finalDownloadLinkElement == null)
                {
                    Log.Error("Не удалось найти финальный элемент для получения ссылки.");
                    await browser.CloseAsync();
                    return null;
                }

                string downloadLink = await finalDownloadLinkElement.GetAttributeAsync("href");
                Log.Debug($"Получена ссылка: {downloadLink}");

                if (downloadLink == "#")
                {
                    Log.Debug("Ссылка недействительна. Попытка повторного клика...");
                    try
                    {
                        await finalDownloadLinkElement.ClickAsync();
                        Log.Debug("Повторный клик выполнен.");
                    }
                    catch (PlaywrightException ex)
                    {
                        Log.Error(ex, "Ошибка при повторном клике на элемент.");
                        await page.EvaluateAsync("arguments[0].click();", finalDownloadLinkElement);
                    }
                    await Task.Delay(3000);
                    downloadLink = await finalDownloadLinkElement.GetAttributeAsync("href");
                    Log.Debug($"После повторного клика получена ссылка: {downloadLink}");
                }

                await browser.CloseAsync();
                Log.Debug("Браузер закрыт.");

                return string.IsNullOrEmpty(downloadLink) ? null : downloadLink;
            }
        }

        private static async Task PerformAction(IPage page, ElementAction action, string videoUrl)
        {
            string selector = GetSelector(action);
            Log.Debug($"Поиск элемента с селектором: {selector}");
            IElementHandle element = await page.WaitForSelectorAsync(selector);

            if (element == null)
            {
                Log.Error($"Элемент не найден: {selector}");
                return;
            }

            Log.Debug($"Элемент найден: {selector}");

            switch (action.Action.ToLower())
            {
                case "fill":
                    Log.Debug($"Заполнение элемента '{selector}' значением: {action.InputValue.Replace("{videoUrl}", videoUrl)}");
                    await element.FillAsync(action.InputValue.Replace("{videoUrl}", videoUrl));
                    break;

                case "click":
                    Log.Debug($"Клик по элементу: {selector}");
                    await element.ClickAsync();
                    break;

                case "queryselector":
                    Log.Debug($"QuerySelector выполнен для элемента: {selector}");
                    break;

                case "getattribute":
                    Log.Debug($"Получение атрибута '{action.Attribute}' для элемента: {selector}");
                    string attributeValue = await element.GetAttributeAsync(action.Attribute);
                    Log.Debug($"Значение атрибута: {attributeValue}");
                    break;

                default:
                    Log.Error($"Неподдерживаемое действие: {action.Action}");
                    throw new ArgumentException($"Unsupported action: {action.Action}");
            }

            if (action.Delay > 0)
            {
                Log.Debug($"Ожидание задержки: {action.Delay} мс");
                await Task.Delay(action.Delay);
            }

            if (action.Condition != null && action.Attribute != null)
            {
                string attributeValue = await element.GetAttributeAsync(action.Attribute);
                Log.Debug($"Проверка условия для атрибута '{action.Attribute}': ожидаемое значение = {action.Condition.CheckValue}, текущее значение = {attributeValue}");
                if (attributeValue == action.Condition.CheckValue)
                {
                    Log.Debug("Условие выполнено. Выполнение дополнительного действия...");
                    await PerformConditionAction(page, element, action.Condition);
                }
            }
        }

        private static async Task PerformConditionAction(IPage page, IElementHandle element, ConditionAction condition)
        {
            Log.Debug($"Выполнение условия: {condition.ActionIfTrue}");
            switch (condition.ActionIfTrue.ToLower())
            {
                case "click":
                    Log.Debug("Клик по элементу для выполнения условия...");
                    await element.ClickAsync();
                    break;

                default:
                    Log.Error($"Неподдерживаемое действие для условия: {condition.ActionIfTrue}");
                    throw new ArgumentException($"Unsupported condition action: {condition.ActionIfTrue}");
            }

            if (condition.DelayAfterAction > 0)
            {
                Log.Debug($"Ожидание задержки после выполнения условия: {condition.DelayAfterAction} мс");
                await Task.Delay(condition.DelayAfterAction);
            }
        }

        private static string GetSelector(ElementAction action)
        {
            switch (action.Type.ToLower())
            {
                case "id":
                    return $"#{action.Value}";
                case "class":
                    return $".{action.Value}";
                default:
                    Log.Error($"Неподдерживаемый тип селектора: {action.Type}");
                    throw new ArgumentException("Unsupported selector type.");
            }
        }
    }

    public class ElementAction
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string Action { get; set; }
        public string InputValue { get; set; }
        public int Delay { get; set; }
        public string Attribute { get; set; }
        public ConditionAction Condition { get; set; }
    }

    public class ConditionAction
    {
        public string CheckValue { get; set; }
        public string ActionIfTrue { get; set; }
        public int DelayAfterAction { get; set; }
        public bool RecheckAttribute { get; set; }
    }
}