namespace TelegramMediaRelayBot.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Infrastructure(string code, string message) => new(code, message, ErrorType.Infrastructure);
    public static Error External(string code, string message) => new(code, message, ErrorType.External);
}

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Infrastructure,
    External
}
