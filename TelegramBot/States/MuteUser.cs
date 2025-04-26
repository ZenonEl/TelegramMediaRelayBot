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

public class ProcessUserMuteState : IUserState
{
    public UserMuteState currentState;

    private int mutedByUserId { get; set; }
    private int mutedContactId { get; set; }
    private DateTime? expirationDate { get; set; }
    private IContactAdder _contactAdder;
    private IContactGetter _contactGetter;

    public ProcessUserMuteState(
        IContactAdder contactAdder,
        IContactGetter contactGetters)
    {
        currentState = UserMuteState.WaitingForLinkOrID;
        _contactAdder = contactAdder;
        _contactGetter = contactGetters;
    }

    public static UserMuteState[] GetAllStates()
    {
        return (UserMuteState[])Enum.GetValues(typeof(UserMuteState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        if (!TGBot.userStates.TryGetValue(chatId, out IUserState? value))
        {
            return;
        }

        var userState = (ProcessUserMuteState)value;

        switch (userState.currentState)
        {
            case UserMuteState.WaitingForLinkOrID:
                int contactId;
                if (int.TryParse(update.Message!.Text, out contactId))
                {
                    List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));
                    string name = DBforGetters.GetUserNameByID(contactId);
                    if (name == "" || !allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
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
                    List<long> allowedIds = await _contactGetter.GetAllContactUserTGIds(DBforGetters.GetUserIDbyTelegramID(update.Message.Chat.Id));

                    if (contactId == -1 || !allowedIds.Contains(DBforGetters.GetTelegramIDbyUserID(contactId)))
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                        return;
                    }
                    string name = DBforGetters.GetUserNameByID(contactId);
                    await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("WillWorkWithContact"), contactId, name), cancellationToken: cancellationToken);
                }
                mutedByUserId = DBforGetters.GetUserIDbyTelegramID(chatId);
                mutedContactId = contactId;
                await botClient.SendMessage(chatId, Config.GetResourceString("ConfirmDecision"), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));
                userState.currentState = UserMuteState.WaitingForConfirmation;
                break;

            case UserMuteState.WaitingForConfirmation:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;

                string text = Config.GetResourceString("MuteTimeInstructions");
                await botClient.SendMessage(chatId, text, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("IndefinitelyButtonText")));
                userState.currentState = UserMuteState.WaitingForMuteTime;
                break;

            case UserMuteState.WaitingForMuteTime:
                string muteTime = update.Message!.Text!;

                if (int.TryParse(muteTime, out int time))
                {
                    DateTime unmuteTime = DateTime.Now.AddSeconds(time);
                    expirationDate = unmuteTime;
                    string unmuteMessage = string.Format(Config.GetResourceString("UserWillBeUnmuted"), unmuteTime, time);
                    await botClient.SendMessage(chatId, unmuteMessage, cancellationToken: cancellationToken);
                }
                else if (DateTime.TryParse(muteTime, out DateTime specifiedDate))
                {
                    expirationDate = specifiedDate;
                    await botClient.SendMessage(chatId, string.Format(Config.GetResourceString("UserWillBeUnmutedAt"), specifiedDate), cancellationToken: cancellationToken);
                }
                else if (muteTime.Equals(Config.GetResourceString("IndefinitelyButtonText"), StringComparison.OrdinalIgnoreCase))
                {
                    expirationDate = null;
                    await botClient.SendMessage(chatId, Config.GetResourceString("UserWillBeMutedIndefinitely"), cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(chatId, Config.GetResourceString("InvalidMuteTimeFormat"), cancellationToken: cancellationToken);
                    return;
                }
                await botClient.SendMessage(chatId, Config.GetResourceString("ConfirmFinalDecision"), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));
                userState.currentState = UserMuteState.Finish;
                break;

            case UserMuteState.Finish:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);

                TGBot.userStates.Remove(chatId);
                if (_contactAdder.AddMutedContact(mutedByUserId, mutedContactId, expirationDate))
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("ActionCancelledError"));
                    return;
                }
                await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("MuteSet"));
                break;
        }
    }
}