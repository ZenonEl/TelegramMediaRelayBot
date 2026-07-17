// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Extensions.Hosting;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramMediaRelayBot.TelegramBot;

/// <summary>
/// Supervised long-polling loop: polling can never die silently.
/// Any crash is logged and polling restarts after a cooldown.
/// </summary>
public class PollingService : BackgroundService
{
    private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NetworkErrorDelay = TimeSpan.FromSeconds(2);

    private readonly ITelegramBotClient _botClient;
    private readonly TGBot _tgBot;
    private bool _announced;

    public PollingService(ITelegramBotClient botClient, TGBot tgBot)
    {
        _botClient = botClient;
        _tgBot = tgBot;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TGBot.cancellationToken = stoppingToken;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_announced)
                {
                    var me = await _botClient.GetMe(stoppingToken);
                    Log.Information($"Hello, I am {me.Id} ready and my name is {me.FirstName}.");
                    _announced = true;
                }

                // A leftover webhook makes getUpdates return 409 forever.
                await _botClient.DeleteWebhook(cancellationToken: stoppingToken);

                await _botClient.ReceiveAsync(
                    updateHandler: _tgBot.UpdateHandler,
                    errorHandler: HandleErrorAsync,
                    receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // host shutdown
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Polling loop crashed; restarting in {Delay}s", RestartDelay.TotalSeconds);
                try { await Task.Delay(RestartDelay, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }

        Log.Information("Polling stopped.");
    }

    // Must never throw: an exception escaping this handler kills the receive loop.
    private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            Log.Error(exception, "Telegram polling error");
            if (exception is RequestException)
                await Task.Delay(NetworkErrorDelay, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception loggingEx)
        {
            Console.Error.WriteLine(loggingEx);
        }
    }
}
