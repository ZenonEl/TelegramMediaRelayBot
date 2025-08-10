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

/// <summary>
/// Adds a contact by a shared link. Minimal UX: user sends link -> preview summary -> confirm/cancel.
/// Uses inline keyboards only and supports /start bailout at any time.
/// </summary>
public class ProcessContactState : IUserState
{
    private string link = string.Empty;
    private int? _foundContactId;
    private string? _foundUserName;
    private readonly IContactAdder _contactAdder;
    private readonly IContactGetter _contactGetter;
    private readonly IUserGetter _userGetter;
    private readonly IPrivacySettingsGetter _privacySettingsGetter;
    private readonly TelegramMediaRelayBot.Config.Services.IResourceService _resourceService;

    public ContactState currentState;

    public ProcessContactState(
        IContactAdder contactAdder,
        IContactGetter contactGetter,
        IUserGetter userGetter,
        IPrivacySettingsGetter privacySettingsGetter,
        TelegramMediaRelayBot.Config.Services.IResourceService resourceService)
    {
        currentState = ContactState.WaitingForLink;
        _contactAdder = contactAdder;
        _contactGetter = contactGetter; 
        _userGetter = userGetter;
        _privacySettingsGetter = privacySettingsGetter;
        _resourceService = resourceService;
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

        // Глобальный выход из стейта
        if (await CommonUtilities.HandleStateBreakCommand(botClient, update, chatId, removeReplyMarkup: false))
        {
            return;
        }

        switch (currentState)
        {
            case ContactState.WaitingForLink:
                // Берём ссылку из текста
                link = update.Message?.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(link))
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("InputErrorMessage"));
                    return;
                }

                // Ищем пользователя по ссылке
                int contactId = await _contactGetter.GetContactIDByLinkAsync(link);
                if (contactId == -1)
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("NoUserFoundByLink"));
                    return;
                }

                // Проверка приватности поиска
                string privacyRuleValue = await _privacySettingsGetter.GetPrivacyRuleValue(contactId, PrivacyRuleType.WHO_CAN_FIND_ME_BY_LINK);
                if (privacyRuleValue == PrivacyRuleAction.NOBODY_CAN_FIND_ME_BY_LINK)
                {
                    await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("NoUserFoundByLink"));
                    return;
                }
                else if (privacyRuleValue == PrivacyRuleAction.GENERAL_CAN_FIND_ME_BY_LINK)
                {
                    int userId = _userGetter.GetUserIDbyTelegramID(chatId);
                    List<int> contacts1 = await _contactGetter.GetAllContactUserIds(userId);
                    List<int> contacts2 = await _contactGetter.GetAllContactUserIds(contactId);
                    bool hasCommon = contacts2.Any(new HashSet<int>(contacts1).Contains);
                    if (!hasCommon)
                    {
                        await CommonUtilities.AlertMessageAndShowMenu(botClient, update, chatId, _resourceService.GetResourceString("NoUserFoundByLink"));
                        return;
                    }
                }

                // Кешируем найденные данные
                _foundContactId = contactId;
                _foundUserName = _userGetter.GetUserNameByID(contactId);

                // Сводка и подтверждение (инлайн)
                string summary = $"{_resourceService.GetResourceString("LinkText")}: {link} \n{_resourceService.GetResourceString("NameText")}: {_foundUserName}";
                await botClient.SendMessage(
                    chatId,
                    _resourceService.GetResourceString("ConfirmAdditionText") + summary,
                    cancellationToken: cancellationToken,
                    replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup()
                );

                currentState = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForName:
                // Больше не используется: сокращённый flow сразу переходит к подтверждению
                currentState = ContactState.WaitingForConfirmation;
                break;

            case ContactState.WaitingForConfirmation:
                if (update.CallbackQuery == null)
                {
                    return;
                }
                var cb = update.CallbackQuery.Data;
                if (cb == "accept")
                {
                    _contactAdder.AddContact(chatId, link);
                    await SendNotification(botClient, chatId, cancellationToken);
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken, _resourceService.GetResourceString("WaitForContactConfirmation"));
                }
                else
                {
                    await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                }
                TGBot.StateManager.Remove(chatId);
                currentState = ContactState.FinishAddContact;
                break;

            case ContactState.FinishAddContact:
                // Уже завершено выше; дублирующее срабатывание — просто выйти в меню
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                TGBot.StateManager.Remove(chatId);
                break;
        }
    }

    public async Task SendNotification(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(_userGetter.GetTelegramIDbyUserID(await _contactGetter.GetContactIDByLinkAsync(link)), 
                                    string.Format(_resourceService.GetResourceString("UserWantsToAddYou"), _userGetter.GetUserNameByTelegramID(chatId)), 
                                    cancellationToken: cancellationToken);
    }
}