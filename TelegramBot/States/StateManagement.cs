namespace TelegramMediaRelayBot.TelegramBot.States;

/// <summary>
/// Универсальный контейнер для хранения данных состояния пользователя.
/// Этот объект будет храниться в IUserStateManager.
/// </summary>
public class UserStateData
{
    /// <summary>
    /// Уникальное имя состояния (например, "MuteUser", "ProcessLink").
    /// </summary>
    public required string StateName { get; init; }

    /// <summary>
    /// Номер текущего шага внутри сценария состояния.
    /// </summary>
    public int Step { get; set; } = 0;

    /// <summary>
    /// Словарь для хранения любых данных, необходимых для сценария.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Базовый интерфейс для всех обработчиков состояний.
/// </summary>
public interface IStateHandler
{
    /// <summary>
    /// Уникальное имя, которое связывает обработчик с UserStateData.StateName.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Обрабатывает один шаг логики состояния.
    /// </summary>
    /// <param name="stateData">Текущие данные состояния пользователя.</param>
    /// <param name="update">Входящее обновление от Telegram.</param>
    /// <param name="botClient">Клиент Telegram.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат обработки.</returns>
    Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken);
}

/// <summary>
/// Представляет результат работы обработчика состояния.
/// </summary>
public enum StateResultAction
{
    /// <summary>
    /// Продолжить сценарий, сохранить измененное состояние.
    /// </summary>
    Continue, 
    /// <summary>
    /// Завершить сценарий, удалить состояние.
    /// </summary>
    Complete,
    /// <summary>
    /// Ничего не делать (например, если обработчик ожидает другой тип Update).
    /// </summary>
    Ignore
}

public class StateResult
{
    public StateResultAction NextAction { get; init; }

    public static StateResult Continue() => new() { NextAction = StateResultAction.Continue };
    public static StateResult Complete() => new() { NextAction = StateResultAction.Complete };
    public static StateResult Ignore() => new() { NextAction = StateResultAction.Ignore };
}