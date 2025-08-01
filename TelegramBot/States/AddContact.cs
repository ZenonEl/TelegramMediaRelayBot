// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.



using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Utils;


namespace TelegramMediaRelayBot;

public class ProcessContactState : IUserState
{
    private string link;
    private readonly IContactAdder _contactAdder;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;

    public ContactState currentState;

    public ProcessContactState(
        IContactAdder contactAdder,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        IPrivacySettingsGetter privacySettingsGetter)
    {
        currentState = ContactState.WaitingForLink;
        _contactAdder = contactAdder;
        _contactGetter = contactGetter; 
        _userGetter = userGetter;
        _privacySettingsGetter = privacySettingsGetter;
    }

    public static ContactState[] GetAllStates()
    {
        return (ContactState[])Enum.GetValues(typeof(ContactState));
    }

    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    public async Task ProcessState(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        long chatId = CommonUtilities.GetIDfromUpdate(update);
        if (CommonUtilities.CheckNonZeroID(chatId)) return;

        switch (currentState)
        {
            case ContactState.WaitingForLink:
                if (update.CallbackQuery != null && update.CallbackQuery.Data == "main_menu")
                {
                    await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                    TGBot.userStates.Remove(chatId);
                    return;
                }

                link = update.Message!.Text!;
                int contactId = _contactGetter.GetContactIDByLink(link);

                if (contactId == -1)
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                    return;
                }

                string privacyRuleValue = await _privacySettingsGetter.GetPrivacyRuleValue(contactId, PrivacyRuleType.WHO_CAN_FIND_ME_BY_LINK);
                if (privacyRuleValue == PrivacyRuleAction.NOBODY_CAN_FIND_ME_BY_LINK)
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                    return;
                }

                else if (privacyRuleValue == PrivacyRuleAction.GENERAL_CAN_FIND_ME_BY_LINK)
                {
                    int userId = _userGetter.GetUserIDbyTelegramID(chatId);
                    List<int> contacts1 = await _contactGetter.GetAllContactUserIds(userId);
                    List<int> contacts2 = await _contactGetter.GetAllContactUserIds(contactId);

                    HashSet<int> contactsSet = new HashSet<int>(contacts1);
                    bool hasCommon = contacts2.Any(contactsSet.Contains);
                    if (!hasCommon)
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, Config.GetResourceString("NoUserFoundByLink"));
                        return;
                    }
                }

                await botClient.SendMessage(chatId, Config.GetResourceString("UserFoundByLink"), cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));

                currentState = ContactState.WaitingForName;
                break;

            case ContactState.WaitingForName:
                string text_data = $"{Config.GetResourceString("LinkText")}: {link} \n{Config.GetResourceString("NameText")}: {_userGetter.GetUserNameByID(_contactGetter.GetContactIDByLink(link))}";

                await botClient.SendMessage(chatId, Config.GetResourceString("ConfirmAdditionText") + text_data, cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));

                currentState = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId)) return;

                _contactAdder.AddContact(chatId, link);

                await SendNotification(botClient, chatId, cancellationToken);
                await botClient.SendMessage(chatId, Config.GetResourceString("WaitForContactConfirmation"),
                                            cancellationToken: cancellationToken,
                                            replyMarkup: ReplyKeyboardUtils.GetSingleButtonKeyboardMarkup(Config.GetResourceString("NextButtonText")));

                currentState = ContactState.FinishAddContact;

                break;

            case ContactState.FinishAddContact:
                await ReplyKeyboardUtils.RemoveReplyMarkup(botClient, chatId, cancellationToken);
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);

                TGBot.userStates.Remove(chatId);
                break;
        }
    }

    public async Task SendNotification(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(_userGetter.GetTelegramIDbyUserID(_contactGetter.GetContactIDByLink(link)), 
                                    string.Format(Config.GetResourceString("UserWantsToAddYou"), _userGetter.GetUserNameByTelegramID(chatId)), 
                                    cancellationToken: cancellationToken);
    }
}