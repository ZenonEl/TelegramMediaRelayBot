// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

// Этот класс становится "пустышкой", которая просто меняет callback_data.
// Основной код будет в InboxListCommand.
public class OpenInboxCommand : IBotCallbackQueryHandlers
{
    private readonly CallbackQueryHandlersFactory _factory;
    public string Name => "open_inbox";

    public OpenInboxCommand(CallbackQueryHandlersFactory factory)
    {
        _factory = factory;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // Вместо создания нового экземпляра, мы просто "перенаправляем" вызов,
        // подменив данные в CallbackQuery. Фабрика сама найдет и создаст нужный InboxListCommand.
        update.CallbackQuery!.Data = "inbox:list:1";
        await _factory.ExecuteAsync(update, botClient, ct);
    }
}
