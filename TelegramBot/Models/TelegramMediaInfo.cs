using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Models;

public class TelegramMediaInfo
{
    public required string FileId { get; init; }
    public TelegramFileType Type { get; init; }
}