using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;

namespace TikTokMediaRelayBot
{
    class TelegramBot
    {
        private static ITelegramBotClient botClient;

        static public async Task Start()
        {
            string telegramBotToken = "5394504584:AAG7tyUSVbufXoni4H2jAKnyvPuwzY-e8Mo";
            botClient = new TelegramBotClient(telegramBotToken);

            var me = await botClient.GetMe();
            Console.WriteLine($"Hello, I am user {me.Id} and my name is {me.FirstName}.");

            var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cancellationToken: cts.Token);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            cts.Cancel();
        }

    private static async Task HandleVideoRequest(ITelegramBotClient botClient, string videoUrl, long chatId)
    {
        string inputId = "s_input";
        string downloadButtonClass = "btn-red";
        string finalDownloadButtonClass = "dl-success";
        string finalDownloadButtonID = "ConvertToVideo";
        int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            string downloadLink = GetDownloadLink(videoUrl, inputId, downloadButtonClass, finalDownloadButtonClass, finalDownloadButtonID);

            if (!string.IsNullOrEmpty(downloadLink))
            {
                await SendVideoToTelegram(downloadLink, chatId);
                return;
            }

            Console.WriteLine($"Attempt {attempt} failed. Retrying...");
            await Task.Delay(2000);
        }

        await botClient.SendMessage(chatId, "Не удалось получить ссылку на видео после 5 попыток.");
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.Text != null)
        {
            string videoUrl = update.Message.Text;
            await HandleVideoRequest(botClient, videoUrl, update.Message.Chat.Id);
        }
    }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Error occurred: {exception.Message}");
            return Task.CompletedTask;
        }

        static string GetDownloadLink(string videoUrl, string inputId, string downloadButtonClass, string finalDownloadButtonClass, string finalDownloadButtonID)
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

                int attempts = 0;
                bool downloadSuccess = false;
                string downloadLink = "";

                while (attempts < 5)
                {
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
                                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                js.ExecuteScript("arguments[0].click();", finalDownloadLinkElement);
                            }

                            Thread.Sleep(5000);

                            downloadLink = finalDownloadLinkElement.GetDomAttribute("href");
                            Console.WriteLine("Ссылка для скачивания: " + downloadLink);
                        }

                        downloadSuccess = true; 
                        attempts = 5;
                    }
                    catch (NoSuchElementException)
                    {
                        attempts++;
                        Console.WriteLine($"Попытка {attempts} не удалась. Повторная попытка...");
                        Thread.Sleep(2000);
                    }
                }

                if (!downloadSuccess)
                {
                    Console.WriteLine("Не удалось найти ссылку для скачивания после 5 попыток.");
                    return null; 
                }

                return downloadLink; 
            }
        }

        static async Task SendVideoToTelegram(string videoUrl, long chatId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(videoUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var videoStream = await response.Content.ReadAsStreamAsync();
                    
                    using (var stream = new MemoryStream())
                    {
                        await videoStream.CopyToAsync(stream);
                        stream.Position = 0; 

                        await botClient.SendDocument(chatId, InputFile.FromStream(stream, "video.mp4"), caption: "Вот ваше видео!");
                        Console.WriteLine("Видео успешно отправлено в Telegram.");
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при скачивании видео: {response.StatusCode}");
                    await botClient.SendMessage(chatId, "Ошибка при скачивании видео.");
                }
            }
        }
    }
}
