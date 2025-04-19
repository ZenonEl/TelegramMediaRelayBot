// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.TelegramBot.Utils;


namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class ViewInboundInviteLinksCommand : IBotCallbackQueryHandlers
{
    public string Name => "view_inbound_invite_links";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, chatId);
    }
}

public class ViewOutboundInviteLinksCommand : IBotCallbackQueryHandlers
{
    public string Name => "view_outbound_invite_links";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await CallbackQueryMenuUtils.ViewOutboundInviteLinks(botClient, update);
    }
}

public class ShowGroupsCommand : IBotCallbackQueryHandlers
{
    public string Name => "show_groups";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Groups.ViewGroups(botClient, update, ct);
    }
}

public class EditContactGroupCommand : IBotCallbackQueryHandlers
{
    public string Name => "edit_contact_group";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.EditContactGroup(botClient, update, chatId);
    }
}

public class ViewContactsCommand : IBotCallbackQueryHandlers
{
    public string Name => "view_contacts";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Contacts.ViewContacts(botClient, update);
    }
}

public class MuteContactCommand : IBotCallbackQueryHandlers
{
    public string Name => "mute_contact";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.MuteUserContact(botClient, update, chatId);
    }
}

public class UnmuteContactCommand : IBotCallbackQueryHandlers
{
    public string Name => "unmute_contact";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.UnMuteUserContact(botClient, update, chatId);
    }
}

public class DeleteContactCommand : IBotCallbackQueryHandlers
{
    public string Name => "delete_contact";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.DeleteContact(botClient, update, chatId);
    }
}