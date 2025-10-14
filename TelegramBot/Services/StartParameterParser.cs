namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IStartParameterParser
{
    string Parse(string messageText);
}

public class StartParameterParser : IStartParameterParser
{
    public string Parse(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return string.Empty;
        }

        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length > 1 && parts[0] == "/start")
        {
            // Мы просто возвращаем второй "кусок" сообщения.
            // Проверку на Guid и т.д. оставим в бизнес-логике, которая его использует.
            return parts[1];
        }

        return string.Empty;
    }
}