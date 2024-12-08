using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace TikTokMediaRelayBot;

public class VideoGet
{
    public static async Task<string> GetDownloadLink(string videoUrl)
    {
        var firefoxOptions = new FirefoxOptions();

        string inputId = Config.inputId;
        string downloadButtonClass = Config.downloadButtonClass;
        string finalDownloadButtonClass = Config.finalDownloadButtonClass;
        string finalDownloadButtonID = Config.finalDownloadButtonID;

        firefoxOptions.SetPreference("network.proxy.type", 1);
        firefoxOptions.SetPreference("network.proxy.socks", "127.0.0.1");
        firefoxOptions.SetPreference("network.proxy.socks_port", 9150);
        firefoxOptions.SetPreference("network.proxy.socks_version", 5);
        firefoxOptions.AddArgument("--headless");

        using (var driver = new FirefoxDriver(firefoxOptions))
        {
            driver.Navigate().GoToUrl("https://tikvideo.app/ru");

            var inputField = driver.FindElement(By.Id(inputId));
            inputField.SendKeys(videoUrl);

            var downloadButton = driver.FindElement(By.ClassName(downloadButtonClass));
            downloadButton.Click();

            await Task.Delay(5000);

            bool downloadSuccess = false;
            string downloadLink = "";

            try
            {
                var finalDownloadLinkElement = driver.FindElement(By.ClassName(finalDownloadButtonClass));
                downloadLink = finalDownloadLinkElement.GetDomAttribute("href");
                if (downloadLink == "#")
                {
                    try
                    {
                        finalDownloadLinkElement.Click();
                    }
                    catch (ElementClickInterceptedException)
                    {
                        IJavaScriptExecutor js = driver;
                        js.ExecuteScript("arguments[0].click();", finalDownloadLinkElement);
                    }

                    await Task.Delay(5000);

                    downloadLink = finalDownloadLinkElement.GetDomAttribute("href");
                }

                downloadSuccess = true; 
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine($"Не удалось найти ссылку для скачивания видео.");
            }

            if (!downloadSuccess)
            {
                return null; 
            }

            return downloadLink; 
        }
    }
}
