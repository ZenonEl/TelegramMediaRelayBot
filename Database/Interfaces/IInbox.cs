// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

namespace TelegramMediaRelayBot.Database.Interfaces
{
    public interface IInboxRepository
    {
        Task<long> AddItemAsync(int ownerUserId, int fromContactId, string caption, string payloadJson, string status);
        Task<bool> SetStatusAsync(long inboxItemId, string status);
        Task<IEnumerable<InboxItemDto>> GetItemsAsync(int ownerUserId, int limit = 20, int offset = 0);
        Task<IEnumerable<InboxItemDto>> GetItemsAsync(int ownerUserId, string? statusFilter, int? fromContactId, int limit = 20, int offset = 0);
        Task<InboxItemDto?> GetItemAsync(long id);
        Task<bool> DeleteAsync(long id);
        Task<int> GetNewCountAsync(int ownerUserId);

        // Bulk operations
        Task<int> SetStatusForOwnerAsync(int ownerUserId, string fromStatus, string toStatus, int? fromContactId = null);
        Task<int> DeleteForOwnerAsync(int ownerUserId, string? statusFilter = null, int? fromContactId = null);

        // Aggregations
        Task<IEnumerable<InboxSenderInfo>> GetSendersAsync(int ownerUserId);

        // Helpers
        Task<InboxItemDto?> GetLatestItemForOwnerFromAsync(int ownerUserId, int fromContactId);
        Task<bool> UpdatePayloadAsync(long inboxItemId, string payloadJson);
    }

    public sealed class InboxItemDto
    {
        public long Id { get; set; }
        public int OwnerUserId { get; set; }
        public int FromContactId { get; set; }
        public string? Caption { get; set; }
        public string PayloadJson { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public DateTime CreatedAt { get; set; }
        // For future extensions (viewed timestamp etc.)
    }

    public sealed class InboxSenderInfo
    {
        public int FromContactId { get; set; }
        public int Total { get; set; }
        public int NewCount { get; set; }
    }
}

