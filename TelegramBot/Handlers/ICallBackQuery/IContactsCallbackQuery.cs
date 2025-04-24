// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Menu;
using TelegramMediaRelayBot.TelegramBot.Utils;


namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;


public class AddContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactAdder _contactRepository;

    public AddContactCommand(IContactAdder contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public string Name => "add_contact";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Contacts.AddContact(botClient, update, update.CallbackQuery!.Message!.Chat.Id, _contactRepository);
    }
}


public class ViewInboundInviteLinksCommand : IBotCallbackQueryHandlers
{
    private readonly IContactSetter _contactSetterRepository;
    private readonly IContactRemover _contactRepository;

    public ViewInboundInviteLinksCommand(
        IContactSetter contactSetterRepository, 
        IContactRemover contactRepository)
    {
        _contactSetterRepository = contactSetterRepository;
        _contactRepository = contactRepository;
    }

    public string Name => "view_inbound_invite_links";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, chatId, _contactSetterRepository, _contactRepository);
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
    private readonly IContactGroupRepository _contactGroupRepository;

    public EditContactGroupCommand(IContactGroupRepository contactGroupRepository)
    {
        _contactGroupRepository = contactGroupRepository;
    }

    public string Name => "edit_contact_group";

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.EditContactGroup(botClient, update, chatId, _contactGroupRepository);
    }
}

public class ViewContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactGetter _contactGetterRepository;

    public ViewContactsCommand(
        IContactGetter contactGetterRepository)
    {
        _contactGetterRepository = contactGetterRepository;
    }

    public string Name => "view_contacts";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await Contacts.ViewContacts(botClient, update, _contactGetterRepository);
    }
}

public class MuteContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactAdder _contactAdderRepository;
    private readonly IContactGetter _contactGetterRepository;

    public MuteContactCommand(
        IContactAdder contactAdderRepository,
        IContactGetter contactGetterRepository)
    {
        _contactAdderRepository = contactAdderRepository;
        _contactGetterRepository = contactGetterRepository;
    }

    public string Name => "mute_contact";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.MuteUserContact(botClient, update, chatId, _contactAdderRepository, _contactGetterRepository);
    }
}

public class UnmuteContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;

    public UnmuteContactCommand(
        IContactRemover contactRepository,
        IContactGetter contactGetterRepository)
    {
        _contactRemoverRepository = contactRepository;
        _contactGetterRepository = contactGetterRepository;
    }

    public string Name => "unmute_contact";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.UnMuteUserContact(botClient, update, chatId, _contactRemoverRepository, _contactGetterRepository);
    }
}

public class DeleteContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactRemover _contactRemoverRepository;
    private readonly IContactGetter _contactGetterRepository;

    public DeleteContactCommand(
        IContactRemover contactRemoverRepository,
        IContactGetter contactGetterRepository)
    {
        _contactRemoverRepository = contactRemoverRepository;
        _contactGetterRepository = contactGetterRepository;
    }

    public string Name => "delete_contact";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        await Contacts.DeleteContact(botClient, update, chatId, _contactRemoverRepository, _contactGetterRepository);
    }
}