using Microsoft.Playwright;
using System.Threading.Tasks;

namespace TikTokMediaRelayBot;


public class VideoGet
{
    public static async Task<string> GetDownloadLink(string videoUrl)
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

            Console.WriteLine("Браузер инициализирован. Открытие страницы...");
            await page.GotoAsync("https://tikvideo.app/ru");

            Console.WriteLine("Ввод URL видео...");
            await page.FillAsync($"#{Config.inputId}", videoUrl);
            try
            {
                await page.ClickAsync($".{Config.downloadButtonClass}");
            }
            catch (PlaywrightException)
            {
                await page.EvaluateAsync("arguments[0].click();", await page.QuerySelectorAsync($".{Config.downloadButtonClass}"));
            }

            Console.WriteLine("Ожидание загрузки...");
            await Task.Delay(5000);
            // await page.WaitForSelectorAsync($".{Config.finalDownloadButtonClass}", new PageWaitForSelectorOptions { Timeout = 1000 });

            string downloadLink = "";
            try
            {
                var finalDownloadLinkElement = await page.QuerySelectorAsync($".{Config.finalDownloadButtonClass}");
                downloadLink = await finalDownloadLinkElement.GetAttributeAsync("href");
                Console.WriteLine($"Ссылка на скачивание1111111111111: {downloadLink}");
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
                    Console.WriteLine("Ожидание загрузки..2222222222.");
                    await Task.Delay(3000);
                    downloadLink = await finalDownloadLinkElement.GetAttributeAsync("href");
                }
            }
            catch (PlaywrightException)
            {
                Console.WriteLine("Не удалось найти ссылку для скачивания видео.");
            }

            Console.WriteLine($"Ссылка на скачивание: {downloadLink}");
            await browser.CloseAsync();

            return string.IsNullOrEmpty(downloadLink) ? null : downloadLink;
        }
    }
}

