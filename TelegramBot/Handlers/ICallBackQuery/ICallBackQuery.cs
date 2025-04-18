// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Telegram.Bot.Types;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Menu;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public interface IBotCallbackQueryHandlers
{
    string Name { get; }
    Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct);
}

public class MainMenuCommand : IBotCallbackQueryHandlers
{
    public string Name => "main_menu";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, ct);
    }
}

public class AddContactCommand : IBotCallbackQueryHandlers
{
    public string Name => "add_contact";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Contacts.AddContact(botClient, update, update.CallbackQuery!.Message!.Chat.Id);
    }
}

public class GetSelfLinkCommand : IBotCallbackQueryHandlers
{
    public string Name => "get_self_link";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CallbackQueryMenuUtils.GetSelfLink(botClient, update);
    }
}

public class WhosTheGeniusCommand : IBotCallbackQueryHandlers
{
    public string Name => "whos_the_genius";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CallbackQueryMenuUtils.WhosTheGenius(botClient, update);
    }
}

