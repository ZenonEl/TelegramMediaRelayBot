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
    private readonly IUiResourceService _uiResources;
    private readonly IHelpResourceService _helpResources;
    private readonly IUserGetter _userGetter;
    private readonly IInboundDBGetter _inboundDbGetter;
    private readonly IOutboundDBGetter _outboundDbGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public CallbackQueryMenuService(
        IUiResourceService uiResources,
        IHelpResourceService helpResources,
        IUserGetter userGetter,
        IInboundDBGetter inboundDbGetter,
        IOutboundDBGetter outboundDbGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _uiResources = uiResources;
        _helpResources = helpResources;
        _userGetter = userGetter;
        _inboundDbGetter = inboundDbGetter;
        _outboundDbGetter = outboundDbGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task GetSelfLink(ITelegramBotClient botClient, Update update)
    {
        string link = _userGetter.GetUserSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        User me = await botClient.GetMe();
        string startLink = $"\nhttps://t.me/{me.Username}?start={link}";
        string text = _uiResources.GetString("UI.Format.UserLink") + startLink;
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, text);
    }

    public async Task ViewInboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        string text = _uiResources.GetString("UI.Header.YourInboundInvitations");
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup keyboard = await InBoundKB.GetInboundsKeyboardMarkup(update, _inboundDbGetter, _userGetter);
        await _interactionService.ReplyToUpdate(botClient, update, keyboard, CancellationToken.None, text);
    }

    public async Task ViewOutboundInviteLinks(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        string text = _uiResources.GetString("UI.Header.YourOutboundInvitations");
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup keyboard = await OutBoundKB.GetOutboundKeyboardMarkup(chatId, _outboundDbGetter, _userGetter);
        await _interactionService.ReplyToUpdate(botClient, update, keyboard, CancellationToken.None, text);
    }

    public Task ShowOutboundInvite(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        string userIdStr = update.CallbackQuery!.Data!.Split(':')[1];
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup keyboard = OutBoundKB.GetOutboundActionsKeyboardMarkup(userIdStr);
        return _interactionService.ReplyToUpdate(botClient, update, keyboard, CancellationToken.None, _uiResources.GetString("UI.ChooseAction"));
    }

    public Task WhosTheGenius(ITelegramBotClient botClient, Update update)
    {
        string text = _helpResources.GetString("Help.AboutTheAuthor");
        return _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, text);
    }
}
