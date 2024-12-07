using DataBase;

namespace TikTokMediaRelayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Config.loadConfig();
            CoreDB.initDB();
            await MediaTelegramBot.TelegramBot.Start();
        }
    }
}