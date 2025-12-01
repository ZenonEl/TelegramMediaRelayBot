// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Text;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;

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
        _updaterTask = Task.Run(UpdateMessageLoop, _cts.Token);
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

                string newContent;
                lock (_logBuffer)
                {
                    if (_logBuffer.Length == 0) continue;
                    newContent = _logBuffer.ToString();
                }

                // --- ИСПРАВЛЕНИЕ ---
                // 1. Убираем очистку буфера здесь. Будем очищать только после успешной отправки.
                // _logBuffer.Clear();

                // 2. Обрезаем до того, как обернуть в ```, чтобы избежать проблем с форматированием
                if (newContent.Length > 4000) newContent = "...\n" + newContent[^4000..];

                var textToSend = $"```{newContent}```";

                if (textToSend == _lastSentText) continue;

                await _botClient.EditMessageText(
                    _chatId, _statusMessage, textToSend,
                    parseMode: ParseMode.MarkdownV2, cancellationToken: _cts.Token,
                    replyMarkup: KeyboardUtils.GetCancelKeyboardMarkup(_statusMessage));

                _lastSentText = textToSend;
                // Очищаем буфер только после успешной отправки
                lock (_logBuffer) { _logBuffer.Clear(); }
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
