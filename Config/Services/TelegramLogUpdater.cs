using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public class TelegramLogUpdater : IAsyncDisposable
{
    private readonly ITelegramBotClient _botClient;
    private readonly MessageId _statusMessage;
    private readonly ChatId _chatId;
    private readonly CancellationTokenSource _cts;
    private readonly StringBuilder _logBuffer = new();
    private readonly int _updateIntervalMs;
    private Task? _updaterTask;
    private string _lastSentText = "";

    public TelegramLogUpdater(ITelegramBotClient botClient, MessageId statusMessage, ChatId chatId, int updateIntervalMs = 2500)
    {
        _botClient = botClient;
        _statusMessage = statusMessage;
        _chatId = chatId;
        _updateIntervalMs = updateIntervalMs;
        _cts = new CancellationTokenSource();
    }

    public void Start()
    {
        _updaterTask = Task.Run(UpdateMessageLoop);
    }

    public void HandleLogLine(string line)
    {
        lock (_logBuffer)
        {
            _logBuffer.AppendLine(line);
        }
    }

    private async Task UpdateMessageLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_updateIntervalMs, _cts.Token);

                string textToSend;
                lock (_logBuffer)
                {
                    if (_logBuffer.Length == 0) continue;
                    textToSend = _logBuffer.ToString();
                    _logBuffer.Clear(); // Очищаем буфер после того, как забрали из него текст
                }
                
                // Обрезаем, если текст слишком длинный
                if (textToSend.Length > 4000) textToSend = textToSend[^4000..];
                
                if (textToSend != _lastSentText)
                {
                    await _botClient.EditMessageText(
                        _chatId,
                        _statusMessage,
                        $"```{textToSend}```", // Отправляем как monospaced-блок
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                        cancellationToken: _cts.Token
                    );
                    _lastSentText = textToSend;
                }
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                // Игнорируем ошибку, если сообщение не изменилось
            }
            catch (OperationCanceledException)
            {
                break; // Нормальное завершение
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to update Telegram log message.");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_updaterTask != null)
        {
            try
            {
                await _updaterTask;
            }
            catch { /* Игнорируем ошибки при завершении */ }
        }
        _cts.Dispose();
    }
}