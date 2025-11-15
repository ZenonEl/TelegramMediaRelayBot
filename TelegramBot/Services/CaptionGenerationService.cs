using System.Text;
using System.Text.RegularExpressions;
using TelegramMediaRelayBot.TelegramBot.Sessions;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ICaptionGenerationService
{
    string Generate(DownloadSession session, string senderName);
}

public class CaptionGenerationService : ICaptionGenerationService
{
    // Регулярное выражение для очистки имени для хештега
    private static readonly Regex _hashtagSanitizer = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

    public string Generate(DownloadSession session, string senderName)
    {
        StringBuilder sb = new StringBuilder();
        
        string sanitizedName = _hashtagSanitizer.Replace(senderName, "_");
        string timestamp = session.OriginalMessageDateUtc.ToString("yyyy_MM_dd_HH_mm_ss");

        sb.AppendLine($"<code>{sanitizedName}</code>");
        sb.AppendLine($"<code>#{timestamp}</code>");
        sb.AppendLine($"#{timestamp}_{sanitizedName}");

        if (!string.IsNullOrWhiteSpace(session.Caption))
        {
            sb.AppendLine();
            sb.Append(session.Caption);
        }

        return sb.ToString();
    }
}