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
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class CreateGroupStateHandler : IStateHandler
{
    private readonly IGroupUoW _groupUoW;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "CreateGroup";

    public CreateGroupStateHandler(IGroupUoW groupUoW, Config.Services.IResourceService resourceService, ITelegramInteractionService interactionService, IStateBreakService stateBreaker)
    {
        _groupUoW = groupUoW;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update)) return StateResult.Complete();

        switch (stateData.Step)
        {
            // ШАГ 0: Ожидание имени группы
            case 0:
                var groupName = update.Message?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    await botClient.SendMessage(chatId, _resourceService.GetResourceString("InputErrorMessage"), cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }
                
                stateData.Data["GroupName"] = groupName;
                await botClient.SendMessage(chatId, _resourceService.GetResourceString("ConfirmDecision"), 
                    replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup(), cancellationToken: cancellationToken);
                
                stateData.Step = 1;
                return StateResult.Continue();

            // ШАГ 1: Ожидание подтверждения
            case 1:
                if (update.CallbackQuery?.Data != "accept")
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, _resourceService.GetResourceString("InvisibleLetter"));
                    return StateResult.Complete();
                }
                
                var finalGroupName = (string)stateData.Data["GroupName"];
                var userId = (int)stateData.Data["UserId"]; // Мы положим это при инициализации
                
                var success = await _groupUoW.SetNewGroup(userId, finalGroupName, "");
                var text = success ? _resourceService.GetResourceString("SuccessActionResult") : _resourceService.GetResourceString("ErrorActionResult");
                
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), CancellationToken.None, text);
                return StateResult.Complete();
        }
        return StateResult.Ignore();
    }
}