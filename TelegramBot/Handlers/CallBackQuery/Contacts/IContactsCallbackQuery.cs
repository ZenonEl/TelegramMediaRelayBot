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
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Services;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class AddContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactMenuService _contactMenuService;
    public string Name => "add_contact";

    public AddContactCommand(IContactMenuService contactMenuService)
    {
        _contactMenuService = contactMenuService;
    }

    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _contactMenuService.StartAddContactFlow(botClient, update);
    }
}

public class ViewInboundInvitesCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IInboundDBGetter _inboundDbGetter;
    private readonly IUserGetter _userGetter;
    private readonly ICallbackQueryMenuService _menuUtils;

    public ViewInboundInvitesCommand(
        IUserStateManager stateManager,
        IInboundDBGetter inboundDbGetter,
        IUserGetter userGetter,
        ICallbackQueryMenuService menuUtils)
    {
        _stateManager = stateManager;
        _inboundDbGetter = inboundDbGetter;
        _userGetter = userGetter;
        _menuUtils = menuUtils;
    }

    public string Name => "view_inbound_invite_links";
    
    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        await _menuUtils.ViewInboundInviteLinks(botClient, update);
        
        var chatId = update.CallbackQuery!.Message!.Chat.Id;
        var newState = new UserStateData { StateName = "InboundInvite", Step = 0 };
        _stateManager.Set(chatId, newState);

        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
    }
}

public class ViewOutboundInviteLinksCommand : IBotCallbackQueryHandlers
{
    private readonly ICallbackQueryMenuService _menuUtils;
    public string Name => "view_outbound_invite_links";

    public ViewOutboundInviteLinksCommand(ICallbackQueryMenuService menuUtils)
    {
        _menuUtils = menuUtils;
    }  
    
    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _menuUtils.ViewOutboundInviteLinks(botClient, update);
    }
}

public class ShowGroupsCommand : IBotCallbackQueryHandlers
{
    private readonly IGroupMenuService _groupMenuService;
    public string Name => "show_groups";

    public ShowGroupsCommand(IGroupMenuService groupMenuService)
    {
        _groupMenuService = groupMenuService;
    }

    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // Заменяем static вызов на вызов сервиса
        return _groupMenuService.ShowGroupsMenu(botClient, update);
    }
}

public class EditContactGroupCommand : IBotCallbackQueryHandlers
{
    private readonly IUserStateManager _stateManager;
    private readonly IGroupMenuService _groupMenuService; // Используем сервис
    public string Name => "edit_contact_group";

    public EditContactGroupCommand(IUserStateManager stateManager, IGroupMenuService groupMenuService)
    {
        _stateManager = stateManager;
        _groupMenuService = groupMenuService;
    }
    
    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        // Передаем всю логику в сервис
        //return _groupMenuService.(botClient, update); 
        return Task.CompletedTask;
    }
}

public class ViewContactsCommand : IBotCallbackQueryHandlers
{
    private readonly IContactMenuService _contactMenuService;
    public string Name => "view_contacts";

    public ViewContactsCommand(IContactMenuService contactMenuService)
    {
        _contactMenuService = contactMenuService;
    }
    
    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _contactMenuService.ViewContacts(botClient, update);
    }
}

public class MuteContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactMenuService _contactMenuService;
    public string Name => "mute_contact";

    public MuteContactCommand(IContactMenuService contactMenuService)
    {
        _contactMenuService = contactMenuService;
    }
    
    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _contactMenuService.StartMuteContactFlow(botClient, update);
    }
}

public class UnmuteContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactMenuService _contactMenuService;
    public string Name => "unmute_contact";

    public UnmuteContactCommand(IContactMenuService contactMenuService)
    {
        _contactMenuService = contactMenuService;
    }
    
    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _contactMenuService.StartUnmuteContactFlow(botClient, update);
    }
}

public class DeleteContactCommand : IBotCallbackQueryHandlers
{
    private readonly IContactMenuService _contactMenuService;
    public string Name => "delete_contact";

    public DeleteContactCommand(IContactMenuService contactMenuService)
    {
        _contactMenuService = contactMenuService;
    }
    
    public Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        return _contactMenuService.StartDeleteContactFlow(botClient, update);
    }
}