// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.


using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Utils;


namespace TelegramMediaRelayBot;

public class ProcessRemoveUser : IUserState
{
    public UsersStandardState currentState;
    private List<int> preparedTargetUserIds = new List<int>();
    private Message statusMessage;
    bool isDeleteSuccessful = false;

    public ProcessRemoveUser(Message statusMessage)
    {
        this.statusMessage = statusMessage;
        currentState = UsersStandardState.ProcessAction;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);

        switch (currentState)
        {
            case UsersStandardState.ProcessAction:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;
                if (update.Message != null)
                {
                    string input = update.Message.Text;
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        await botClient.SendMessage(chatId, Config.GetResourceString("PleaseEnterContactIDs"), cancellationToken: cancellationToken);
                        return;
                    }

                    string[] ids = input.Split(' ');
                    if (ids.All(id => int.TryParse(id, out _)))
                    {
                        preparedTargetUserIds = ids.Select(int.Parse).ToList();
                        bool isSuccessful = await RetrieveAndDisplayUserInfo(botClient, update, chatId, cancellationToken);
                        if (isSuccessful) currentState = UsersStandardState.ProcessData;
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, Config.GetResourceString("InvalidInputNumbers"), cancellationToken: cancellationToken);
                    }
                }
                break;

            case UsersStandardState.ProcessData:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false)) return;
                
                if (update.CallbackQuery != null)
                {
                    string callbackData = update.CallbackQuery.Data!;
                    if (callbackData == "confirm_removal")
                    {
                        RemoveUsersFromContacts(botClient, chatId, cancellationToken);
                        await botClient.EditMessageText(chatId, statusMessage.MessageId, Config.GetResourceString("RemovalProcessCompleted"), cancellationToken: cancellationToken);
                        
                        string text = isDeleteSuccessful ? Config.GetResourceString("SuccessActionResult") : Config.GetResourceString("ErrorActionResult");
                        await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken, text);
                        
                        TGBot.userStates.Remove(chatId);
                    }
                    else if (callbackData == "cancel_removal")
                    {
                        await botClient.EditMessageText(chatId, statusMessage.MessageId, Config.GetResourceString("PleaseEnterContactIDs"), replyMarkup: KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken);
                        currentState = UsersStandardState.ProcessAction;
                    }
                }
                break;
        }
    }

    private async Task<bool> RetrieveAndDisplayUserInfo(ITelegramBotClient botClient, Update update, long chatId, CancellationToken cancellationToken)
    {
        List<long> contactUserTGIds = await ContactGetter.GetAllContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(chatId));
        List<long> preparedTargetUserTGIds = preparedTargetUserIds.Select(id => DBforGetters.GetTelegramIDbyUserID(id)).ToList();

        contactUserTGIds = contactUserTGIds.Intersect(preparedTargetUserTGIds).ToList();
        List<string> contactUsersInfo = new List<string>();

        foreach (var contactUserId in contactUserTGIds)
        {
            int id = DBforGetters.GetUserIDbyTelegramID(contactUserId);
            string username = DBforGetters.GetUserNameByTelegramID(contactUserId);
            string link = DBforGetters.GetSelfLink(contactUserId);

            contactUsersInfo.Add(string.Format(Config.GetResourceString("ContactInfo"), id, username, link));
        }

        if (contactUsersInfo.Any())
        {
            string messageText = $"{Config.GetResourceString("ConfirmRemovalMessage")}\n\n{string.Join("\n", contactUsersInfo)}";
            InlineKeyboardMarkup keyboard = KeyboardUtils.GetConfirmForActionKeyboardMarkup("confirm_removal", "cancel_removal");

            await botClient.EditMessageText(chatId, statusMessage.MessageId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken, parseMode: ParseMode.Html);
            return true;
        }
        else
        {
            await botClient.EditMessageText(chatId, statusMessage.MessageId, Config.GetResourceString("NoUsersFound"), cancellationToken: cancellationToken);
        }
        return false;
    }

    private void RemoveUsersFromContacts(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        int userId = DBforGetters.GetUserIDbyTelegramID(chatId);
        isDeleteSuccessful = ContactRemover.RemoveUsersFromContacts(userId, preparedTargetUserIds);
    }
}
