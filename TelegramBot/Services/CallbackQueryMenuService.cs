// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;
using TelegramMediaRelayBot.TelegramBot.Utils.Keyboard;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ICallbackQueryMenuService
{
    Task GetSelfLink(ITelegramBotClient botClient, Update update);
    Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update);
    Task ViewOutboundInviteLinks(ITelegramBotClient botClient, Update update);
    Task ShowOutboundInvite(ITelegramBotClient botClient, Update update);
    Task WhosTheGenius(ITelegramBotClient botClient, Update update);
}

public class CallbackQueryMenuService : ICallbackQueryMenuService
{
    private readonly IUserGetter _userGetter;
    private readonly IInboundDBGetter _inboundDbGetter;
    private readonly IOutboundDBGetter _outboundDbGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public CallbackQueryMenuService(
        IUserGetter userGetter,
        IInboundDBGetter inboundDbGetter,
        IOutboundDBGetter outboundDbGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _userGetter = userGetter;
        _inboundDbGetter = inboundDbGetter;
        _outboundDbGetter = outboundDbGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task GetSelfLink(ITelegramBotClient botClient, Update update)
    {
        var link = _userGetter.GetUserSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        var me = await botClient.GetMe();
        var startLink = $"\nhttps://t.me/{me.Username}?start={link}";
        var text = _resourceService.GetResourceString("YourLink") + startLink;
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, text);
    }

    public async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        var text = _resourceService.GetResourceString("YourInboundInvitations");
        var keyboard = await InBoundKB.GetInboundsKeyboardMarkup(update, _inboundDbGetter, _userGetter);
        await _interactionService.ReplyToUpdate(botClient, update, keyboard, CancellationToken.None, text);
    }

    public async Task ViewOutboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var text = _resourceService.GetResourceString("YourOutboundInvitations");
        var keyboard = await OutBoundKB.GetOutboundKeyboardMarkup(chatId, _outboundDbGetter, _userGetter);
        await _interactionService.ReplyToUpdate(botClient, update, keyboard, CancellationToken.None, text);
    }

    public Task ShowOutboundInvite(ITelegramBotClient botClient, Update update)
    {
        var chatId = _interactionService.GetChatId(update);
        var userIdStr = update.CallbackQuery!.Data!.Split(':')[1];
        var keyboard = OutBoundKB.GetOutboundActionsKeyboardMarkup(userIdStr);
        return _interactionService.ReplyToUpdate(botClient, update, keyboard, CancellationToken.None, _resourceService.GetResourceString("OutboundInviteMenu"));
    }

    public Task WhosTheGenius(ITelegramBotClient botClient, Update update)
    {
        var text = _resourceService.GetResourceString("WhosTheGeniusText");
        return _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, text);
    }
}