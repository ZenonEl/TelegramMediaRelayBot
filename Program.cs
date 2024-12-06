using DataBase;

namespace TikTokMediaRelayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Config.loadConfig();
            DB.initDB();
            await MediaTelegramBot.TelegramBot.Start();
        }
    }
}