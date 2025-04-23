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
using System.Reflection;

namespace TelegramMediaRelayBot.TelegramBot.Handlers;


public class CallbackQueryHandlersFactory
{
    private readonly Dictionary<string, IBotCallbackQueryHandlers> _commands;

    public CallbackQueryHandlersFactory(IEnumerable<IBotCallbackQueryHandlers> commands)
    {
        _commands = commands.ToDictionary(c => c.Name);
    }

    public IBotCallbackQueryHandlers GetCommand(string commandName)
    {
        if (_commands.TryGetValue(commandName, out var command))
            return command;

        throw new Exception($"CallbackQuery command: {commandName} not found");
    }
}
