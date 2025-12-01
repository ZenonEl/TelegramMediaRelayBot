// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;

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

        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        UserStateData newState = new UserStateData { StateName = "InboundInvite", Step = 0 };
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
        return _groupMenuService.ShowGroupsMenu(botClient, update);
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
