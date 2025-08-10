// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


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
