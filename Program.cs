namespace TikTokMediaRelayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await TelegramBot.Start();
        }
    }
}