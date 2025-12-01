// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramMediaRelayBot.TelegramBot.Handlers;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.States;

namespace TelegramMediaRelayBot.TelegramBot;

public partial class TGBot : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public TGBot(ITelegramBotClient botClient, IServiceScopeFactory scopeFactory)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("TGBot Hosted Service is starting.");

        User me = _botClient.GetMe(cancellationToken).GetAwaiter().GetResult();
        Log.Information("Hello, I am {BotId} ready and my name is {BotName}.", me.Id, me.FirstName);

        _botClient.StartReceiving(
            updateHandler: UpdateHandler,
            errorHandler: ErrorHandler,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cancellationToken
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("TGBot Hosted Service is stopping.");
        return Task.CompletedTask;
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Создаем НОВЫЙ scope для КАЖДОГО `Update`.
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IServiceProvider sp = scope.ServiceProvider;

        // Получаем из scope все сервисы, которые могут понадобиться в этом запросе.
        ITelegramInteractionService interactionService = sp.GetRequiredService<ITelegramInteractionService>();
        IUserStateManager stateManager = sp.GetRequiredService<IUserStateManager>();
        StateHandlerFactory stateHandlerFactory = sp.GetRequiredService<StateHandlerFactory>();

        long chatId = interactionService.GetChatId(update);
        if (chatId == 0) return;

        // Логирование переносим сюда, так как нам нужен stateManager из scope
        LogEvent(update, chatId, stateManager);

        // 1. Проверяем, не находится ли пользователь в состоянии
        if (stateManager.TryGet(chatId, out UserStateData? stateData))
        {
            IStateHandler? handler = stateHandlerFactory.GetHandler(stateData.StateName);
            if (handler != null)
            {
                StateResult result = await handler.Process(stateData, update, botClient, cancellationToken);

                if (result.NextAction == StateResultAction.Complete) stateManager.Remove(chatId);
                else if (result.NextAction == StateResultAction.Continue) stateManager.Set(chatId, stateData);

                return; // Состояние обработано, выходим.
            }
        }

        // 2. Если состояния не было, определяем тип чата и передаем дальше
        if (update.CallbackQuery?.Message?.Chat.Type == ChatType.Private)
        {
            PrivateUpdateHandler privateHandler = sp.GetRequiredService<PrivateUpdateHandler>();
            await privateHandler.ProcessCallbackQuery(botClient, update, cancellationToken);
        }
        else if (update.Message?.Chat.Type == ChatType.Private)
        {
            PrivateUpdateHandler privateHandler = sp.GetRequiredService<PrivateUpdateHandler>();
            await privateHandler.ProcessMessage(botClient, update, cancellationToken);
        }
        else
        {
            GroupUpdateHandler groupHandler = sp.GetRequiredService<GroupUpdateHandler>();
            await groupHandler.HandleGroupUpdate(botClient, update, cancellationToken);
        }
    }

    // Переносим ErrorHandler сюда, чтобы он был доступен
    private Task ErrorHandler(ITelegramBotClient _, Exception exception, CancellationToken __)
    {
        Log.Error(exception, "An error occurred in Telegram Update Receiver.");
        return Task.CompletedTask;
    }

    // Логика логирования теперь тоже здесь, так как она зависит от Scoped-сервиса
    private void LogEvent(Update update, long chatId, IUserStateManager stateManager)
    {
        (string logMessageType, string logMessageData, long userId) = update.Type switch
        {
            UpdateType.Message => ("Message", update.Message!.Text ?? string.Empty, update.Message.From!.Id),
            UpdateType.CallbackQuery => ("CallbackQuery", update.CallbackQuery!.Data ?? string.Empty, update.CallbackQuery.From.Id),
            _ => ("Unknown", string.Empty, 0)
        };

        if (userId == 0) return;

        string? stateName = stateManager.TryGet(chatId, out UserStateData? state) ? state?.StateName : "None";

        Log.Information("Event: {Type}, UserId: {UserId}, ChatId: {ChatId}, Data: {Data}, State: {State}",
            logMessageType, userId, chatId, logMessageData, stateName);
    }
}
