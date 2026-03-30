// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.


namespace TelegramMediaRelayBot.TelegramBot.Sessions;

public class MediaSession
{
    public string SessionId { get; set; }
    public long ChatId { get; set; }
    public string Url { get; set; }
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CancellationTokenSource Cts { get; set; } = new();

    public MediaSession(string sessionId, long chatId, string url, string? caption = null)
    {
        SessionId = sessionId;
        ChatId = chatId;
        Url = url;
        Caption = caption;
    }
}
