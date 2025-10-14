using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Handlers.ICallBackQuery;

public class GetSelfLinkCommand : IBotCallbackQueryHandlers
{
    private readonly IUserGetter _userGetter;
    private readonly IResourceService _recourceService;
    private readonly ITelegramInteractionService _interactionService;
    public string Name => "get_self_link";

    public GetSelfLinkCommand(IUserGetter userGetter, IResourceService resourceService, ITelegramInteractionService interactionService)
    {
        _userGetter = userGetter;
        _recourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task ExecuteAsync(Update update, ITelegramBotClient botClient, CancellationToken ct)
    {
        string link = _userGetter.GetUserSelfLink(update.CallbackQuery!.Message!.Chat.Id);
        User me = await botClient.GetMe();
        string startLink = $"\nhttps://t.me/{me.Username}?start={link}";
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), ct, string.Format(_recourceService.GetResourceString("YourLink") + startLink, link));
    }
}