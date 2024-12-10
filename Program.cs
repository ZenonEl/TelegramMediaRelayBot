using DataBase;
using Serilog;

namespace TikTokMediaRelayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message} {Exception}{NewLine}").CreateLogger();

            try 
            {
                Config.loadConfig();
                CoreDB.initDB();
                Scheduler.Init();
                await MediaTelegramBot.TelegramBot.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Произошла ошибка в методе {MethodName}", nameof(Main));
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}