using System;
using System.Threading;
using System.Threading.Tasks;
using DataBase;
using Serilog;

namespace TikTokMediaRelayBot.Scheduler;

class Scheduler
{
    private static Timer _unMuteTimer;

    public static void Init()
    {
        _unMuteTimer = new Timer(async _ => await CheckForUnmuteContacts(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        Log.Information("Скедулер запушен.", nameof(Scheduler));
    }

    private static async Task CheckForUnmuteContacts()
    {
        try
        {
            List<int> expiredMutes = DBforGetters.GetExpiredMutes();

            foreach (var muteUserId in expiredMutes)
            {
                CoreDB.UnMuteByMuteId(muteUserId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Произошла ошибка в методе {MethodName}", nameof(CheckForUnmuteContacts));
        }
    }
}
