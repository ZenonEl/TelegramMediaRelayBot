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
using TelegramMediaRelayBot.TelegramBot.Utils;


namespace TelegramMediaRelayBot;

public class ProcessRenameContactState : IUserState
{
    public UserRenameContactState currentState;

    private int userId { get; set; }
    private int targetContactId { get; set; }
    private readonly IContactSetter _contactSetter;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;

    public ProcessRenameContactState(
        IContactSetter contactSetter,
        IContactGetter contactGetter,
        IUserGetter userGetter
        )
    {
        currentState = UserRenameContactState.WaitingForLinkOrID;
        _contactSetter = contactSetter;
        _contactGetter = contactGetter;
        _userGetter = userGetter;
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (!UserSessionManager.TryGetValue(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (ProcessRenameContactState)value;

        switch (userState.currentState)
        {
            case UserRenameContactState.WaitingForLinkOrID:
                int contactId;
                if (int.TryParse(update.Message!.Text, out contactId))
                {
                    List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(_userGetter.GetUserIDbyTelegramID(update.Message.Chat.Id));
                    string name = _userGetter.GetUserNameByID(contactId);
                    if (name == "" || !allowedIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByID"));
                        return;
                    }
                    await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken,
                                                replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));
                }
                else
                {
                    string link = update.Message.Text!;
                    contactId = _contactGetter.GetContactIDByLink(link);
                    List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(_userGetter.GetUserIDbyTelegramID(update.Message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(_userGetter.GetTelegramIDbyUserID(contactId)))
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                        return;
                    }
                    string name = _userGetter.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken);
                }
                userState.userId = _userGetter.GetUserIDbyTelegramID(chatId);
                userState.targetContactId = contactId;
                await botClient.SendMessage(chatId, Config.GetResourceString("InputNewDisplayName"), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("ResetDisplayNameButtonText")));
                userState.currentState = UserRenameContactState.WaitingForNewName;
                break;

            case UserRenameContactState.WaitingForNewName:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;

                string newName = update.Message!.Text!;
                string? displayName = newName.Equals(Config.GetResourceString("ResetDisplayNameButtonText"), StringComparison.OrdinalIgnoreCase)
                    ? null
                    : newName;

                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                bool success = _contactSetter.SetContactDisplayName(userState.userId, userState.targetContactId, displayName);
                UserSessionManager.Remove(chatId);

                if (success)
                {
                    string resultText = displayName != null
                        ? string.Format(Config.GetResourceString("DisplayNameSet"), displayName)
                        : Config.GetResourceString("DisplayNameReset");
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, resultText);
                }
                else
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("ActionCancelledError"));
                }
                break;
        }
    }
}
