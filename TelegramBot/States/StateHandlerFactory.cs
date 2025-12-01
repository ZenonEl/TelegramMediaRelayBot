// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.TelegramBot.States;

public class StateHandlerFactory
{
    private readonly IReadOnlyDictionary<string, IStateHandler> _handlers;

    // Фабрика получает все реализации IStateHandler через DI
    public StateHandlerFactory(IEnumerable<IStateHandler> handlers)
    {
        // Создаем словарь для быстрого поиска обработчика по имени
        _handlers = handlers.ToDictionary(h => h.Name, h => h, StringComparer.OrdinalIgnoreCase);
    }

    public IStateHandler? GetHandler(string stateName)
    {
        _handlers.TryGetValue(stateName, out var handler);
        return handler;
    }
}
