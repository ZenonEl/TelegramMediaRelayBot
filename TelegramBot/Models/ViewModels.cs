namespace TelegramMediaRelayBot.TelegramBot.Models;

public class ContactViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Link { get; init; }
    public required string MembershipInfo { get; init; }
}

public class GroupViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int MemberCount { get; init; }
    public bool IsDefault { get; init; }
}