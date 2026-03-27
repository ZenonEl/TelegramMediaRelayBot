namespace TelegramMediaRelayBot.Domain.Common.Errors;

public static class TelegramErrors
{
    public static Error FileTooLarge(long sizeBytes, long limitBytes) =>
        Error.Validation("Telegram.FileTooLarge",
            $"File size {sizeBytes / 1_048_576}MB exceeds limit {limitBytes / 1_048_576}MB");

    public static Error ApiError(string message) =>
        Error.External("Telegram.ApiError", message);

    public static Error UserNotFound(long telegramId) =>
        Error.NotFound("Telegram.UserNotFound", $"User not found: {telegramId}");

    public static Error ChatNotFound(long chatId) =>
        Error.NotFound("Telegram.ChatNotFound", $"Chat not found: {chatId}");
}
