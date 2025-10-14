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
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using Microsoft.Extensions.DependencyInjection;
using TelegramMediaRelayBot.Database.Interfaces; // Убедись, что using для IContactUoW/IUserUoW здесь есть

namespace TelegramMediaRelayBot.TelegramBot;

// 1. Реализуем IHostedService и IDisposable
public class Scheduler : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<MessageDelayConfiguration> _delayConfig;
    private readonly IOptionsMonitor<TorConfiguration> _torConfig;
    
    private Task? _unmuteTask;
    private Task? _torTask;
    private CancellationTokenSource? _stoppingCts;

    public Scheduler(
        IServiceScopeFactory scopeFactory, // Запрашиваем фабрику скоупов
        IOptionsMonitor<MessageDelayConfiguration> delayConfig,
        IOptionsMonitor<TorConfiguration> torConfig)
    {
        _scopeFactory = scopeFactory;
        _delayConfig = delayConfig;
        _torConfig = torConfig;
    }

    // 2. StartAsync - точка входа для IHost
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("Scheduler is starting.");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Запускаем наши фоновые задачи
        _unmuteTask = RunUnmuteLoop(_stoppingCts.Token);
        _torTask = RunTorLoop(_stoppingCts.Token);
        
        return Task.CompletedTask;
    }

    // 3. Бесконечный цикл для задачи разбана
    private async Task RunUnmuteLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Выполняем полезную работу
                await CheckForUnmuteContacts(stoppingToken);
                
                // Ждем интервал из конфига
                var delay = TimeSpan.FromSeconds(_delayConfig.CurrentValue.UserUnMuteCheckInterval);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the unmute loop.");
                // Ждем немного перед следующей попыткой, чтобы не спамить логами при постоянной ошибке
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }

    // 4. Бесконечный цикл для задачи Tor
    private async Task RunTorLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var torConfig = _torConfig.CurrentValue;
                if (torConfig.Enabled)
                {
                    // Выполняем полезную работу
                    await ChangeTorCircuit(stoppingToken);

                    // Ждем интервал из конфига
                    var delay = TimeSpan.FromMinutes(torConfig.TorChangingChainInterval);
                    await Task.Delay(delay, stoppingToken);
                }
                else
                {
                    // Если Tor выключен, просто "засыпаем" надолго и проверяем снова
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in the Tor loop.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    // 5. Логика разбана теперь использует Scoped-сервисы
    private async Task CheckForUnmuteContacts(CancellationToken stoppingToken)
    {
        // IHostedService - это Singleton, а сервисы БД - Scoped.
        // Чтобы безопасно их использовать, нужно создавать Scope.
        using var scope = _scopeFactory.CreateScope();
        var userGetter = scope.ServiceProvider.GetRequiredService<IUserGetter>();
        var contactUow = scope.ServiceProvider.GetRequiredService<IContactUoW>(); // Используем наш новый UoW!

        var expiredMutes = userGetter.GetExpiredUsersMutes();
        foreach (var mute in expiredMutes) // Предполагаем, что GetExpiredUsersMutes возвращает объект с нужными ID
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Вызываем метод из UoW сервиса, который выполнит UPDATE в транзакции
            await contactUow.UnMuteUserByMuteId(mute);
            Log.Information("Mute record {MuteId} deactivated.", mute);
                }
    }
    
    // 6. Логика Tor остается почти без изменений
    private async Task ChangeTorCircuit(CancellationToken stoppingToken)
    {
        var torConfig = _torConfig.CurrentValue;
        
        var controlPortClient = new DotNetTor.ControlPort.Client(
            torConfig.TorSocksHost,
            controlPort: torConfig.TorControlPort,
            password: torConfig.TorControlPassword ?? "");
        
        await controlPortClient.ChangeCircuitAsync(stoppingToken);

        using var httpClient = new HttpClient(new DotNetTor.SocksPort.SocksPortHandler(
            torConfig.TorSocksHost,
            socksPort: torConfig.TorSocksPort));

        var result = await httpClient.GetStringAsync("https://check.torproject.org/api/ip", stoppingToken);
        Log.Debug("New Tor IP: {TorIp}", result);
    }

    // 7. StopAsync - точка выхода
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Scheduler is stopping.");
        
        // Сигнализируем нашим циклам, что пора завершаться
        _stoppingCts?.Cancel();
        
        // Ждем, пока все задачи завершатся (или пока не истечет таймаут)
        if (_unmuteTask != null && _torTask != null)
        {
            await Task.WhenAll(_unmuteTask, _torTask);
        }
    }

    // 8. Освобождаем ресурсы
    public void Dispose()
    {
        _stoppingCts?.Dispose();
    }
}