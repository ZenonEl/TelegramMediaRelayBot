using System.Globalization;
using DataBase;
using Serilog;

namespace TelegramMediaRelayBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Config.LoadConfig();

            CultureInfo currentCulture = CultureInfo.CurrentUICulture;

            if (Config.language != null)
            {
                currentCulture = new CultureInfo(Config.language);
            }

            Thread.CurrentThread.CurrentUICulture = currentCulture;
            Thread.CurrentThread.CurrentCulture = currentCulture;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Config.logLevel)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message} {Exception}{NewLine}").CreateLogger();

            try 
            {
                Log.Information($"Log level: {Config.logLevel}");
                CoreDB.initDB();
                Scheduler.Scheduler.Init();
                await MediaTelegramBot.TelegramBot.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the method{MethodName}", nameof(Main));
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}