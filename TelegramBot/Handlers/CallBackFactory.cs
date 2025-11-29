// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.


using TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramMediaRelayBot.TelegramBot.Handlers;


public class CallbackQueryHandlersFactory
{
    private readonly IServiceProvider _provider;

    public CallbackQueryHandlersFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IBotCallbackQueryHandlers GetCommand(string commandName)
    {
        // Fallback: resolve in a temporary scope for single call usage
        using var scope = _provider.CreateScope();
        var commands = scope.ServiceProvider.GetServices<IBotCallbackQueryHandlers>();
        var command = commands.FirstOrDefault(c => c.Name == commandName);
        if (command is null)
        {
            throw new Exception($"CallbackQuery command: {commandName} not found");
        }
        return command;
    }

    public async Task ExecuteAsync(string commandName, Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider
            .GetServices<IBotCallbackQueryHandlers>()
            .FirstOrDefault(c => c.Name == commandName)
            ?? throw new Exception($"CallbackQuery command: {commandName} not found");
        await handler.ExecuteAsync(update, botClient, ct);
    }
}
