using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace TikTokMediaRelayBot;

public class VideoGet
{
    public static string GetDownloadLink(string videoUrl, string inputId, string downloadButtonClass, string finalDownloadButtonClass, string finalDownloadButtonID)
    {
        var firefoxOptions = new FirefoxOptions();
        
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

            Thread.Sleep(5000);

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

                    Thread.Sleep(5000);

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
