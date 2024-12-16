using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TikTokMediaRelayBot;


namespace MediaTelegramBot.Utils;

public static class Utils
{
    public static Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Log.Error($"Error occurred: {exception.Message}");
        Log.Error($"Stack trace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            Log.Error($"Inner exception: {exception.InnerException.Message}");
            Log.Error($"Inner exception stack trace: {exception.InnerException.StackTrace}");
        }

        return Task.CompletedTask;
    }

    public static long GetIDfromUpdate(Update update)
    {
        if (update == null) return 0;
        if (update.Message != null)
        {
            return update.Message.Chat.Id;
        }
        else if (update.CallbackQuery != null)
        {
            return update.CallbackQuery.Message!.Chat.Id;
        }
        return 0;
    }

    public static bool CheckNonZeroID(long id)
    {
        if (id == 0) return true;
        return false;
    }

    public static bool CheckPrivateChatType(Update update)
    {
        if (update.Message != null && update.Message.Chat.Type == ChatType.Private) return true;
        if (update.CallbackQuery != null && update.CallbackQuery.Message!.Chat.Type == ChatType.Private) return true;
        return false;
    }

    public static Task SendMessage(ITelegramBotClient botClient, Update update, InlineKeyboardMarkup inlineKeyboard,
                                    CancellationToken cancellationToken, string? text = null)
    {
        text ??= Config.resourceManager.GetString("ChooseOptionText", System.Globalization.CultureInfo.CurrentUICulture)!;

        long chatId = GetIDfromUpdate(update);

        if (update.CallbackQuery != null)
        {
            return botClient.EditMessageText(
                chatId: chatId,
                messageId: update.CallbackQuery.Message!.MessageId,
                text: text,
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html
            );
        }

        if (update.Message != null)
        {
            return botClient.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.Html
            );
        }

        return Task.CompletedTask;
    }

    public static async Task AlertMessageAndShowMenu(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, string text)
    {
        await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        TelegramBot.userStates.Remove(chatId);
    }

    public static async Task<bool> HandleStateBreakCommand(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId, string command = "/start")
    {
        if (update.Message!.Text == command)
        {
            await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
            TelegramBot.userStates.Remove(chatId);
            return true;
        }
        return false;
    }

    public static bool IsLink(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out Uri? uriResult)
                            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}


