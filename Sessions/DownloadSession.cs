// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.TelegramBot.Sessions;

/// <summary>
/// Хранит все данные, связанные с одной операцией обработки ссылки.
/// </summary>
public class DownloadSession
{
    public int StatusMessageId { get; set; }
    public long ChatId { get; init; }
    public string Url { get; set; }
    public string Caption { get; set; }
    public DateTime OriginalMessageDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public bool IsProcessing { get; set; } = false;

    /// <summary>
    /// CancellationTokenSource для отмены автоматического действия по умолчанию.
    /// </summary>
    public CancellationTokenSource? DefaultActionCts { get; set; }

    /// <summary>
    /// CancellationTokenSource для отмены всей сессии (скачивания и отправки).
    /// </summary>
    public CancellationTokenSource SessionCts { get; } = new();
}