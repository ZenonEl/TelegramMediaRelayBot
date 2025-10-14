namespace TelegramMediaRelayBot.TelegramBot.States;

public class StateHandlerFactory
{
    private readonly IReadOnlyDictionary<string, IStateHandler> _handlers;

    // Фабрика получает все реализации IStateHandler через DI
    public StateHandlerFactory(IEnumerable<IStateHandler> handlers)
    {
        // Создаем словарь для быстрого поиска обработчика по имени
        _handlers = handlers.ToDictionary(h => h.Name, h => h, StringComparer.OrdinalIgnoreCase);
    }

    public IStateHandler? GetHandler(string stateName)
    {
        _handlers.TryGetValue(stateName, out var handler);
        return handler;
    }
}