using Microsoft.Playwright;
using Serilog;
using System.Threading.Tasks;

namespace TikTokMediaRelayBot;


public class VideoGet
{
    public static async Task<string?> GetDownloadLink(string videoUrl)
    {
        using (var playwright = await Playwright.CreateAsync().ConfigureAwait(false))
        {
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = true,
                Proxy = new Proxy
                {
                    Server = "socks5://localhost:9150"
                }
            };

            var browser = await playwright.Firefox.LaunchAsync(launchOptions);
            var page = await browser.NewPageAsync();

            Log.Debug("Браузер инициализирован. Открытие страницы...");
            await page.GotoAsync("https://tikvideo.app/ru");

            Log.Debug("Ввод URL видео...");
            await page.FillAsync($"#{Config.inputId}", videoUrl);

            try
            {
                await page.ClickAsync($".{Config.downloadButtonClass}");
            }
            catch (PlaywrightException)
            {
                await page.EvaluateAsync("arguments[0].click();", await page.QuerySelectorAsync($".{Config.downloadButtonClass}"));
            }

            Log.Debug("Ожидание загрузки...");
            await Task.Delay(5000);

            string downloadLink = "";
            try
            {
                var finalDownloadLinkElement = await page.QuerySelectorAsync($".{Config.finalDownloadButtonClass}");
                downloadLink = await finalDownloadLinkElement.GetAttributeAsync("href");
                if (downloadLink == "#")
                {
                    try
                    {
                        await finalDownloadLinkElement.ClickAsync();
                    }
                    catch (PlaywrightException)
                    {
                        await page.EvaluateAsync("arguments[0].click();", finalDownloadLinkElement);
                    }
                    await Task.Delay(3000);
                    downloadLink = await finalDownloadLinkElement.GetAttributeAsync("href");
                }
            }
            catch (PlaywrightException)
            {
                Log.Error("Не удалось найти ссылку для скачивания видео. {MethodName}", nameof(GetDownloadLink));
            }

            await browser.CloseAsync();

            return string.IsNullOrEmpty(downloadLink) ? null : downloadLink;
        }
    }
}

