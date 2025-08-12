using FluentValidation;
using Telegram.Bot.Types;

namespace TelegramMediaRelayBot.TelegramBot.Validation;

public sealed class InboxListRequest
{
    public long ChatId { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; } = 10;
}

public sealed class InboxViewRequest
{
    public long ChatId { get; init; }
    public long ItemId { get; init; }
    public int Page { get; init; }
}

public sealed class InboxListRequestValidator : AbstractValidator<InboxListRequest>
{
    public InboxListRequestValidator()
    {
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(5, 50);
    }
}

public sealed class InboxViewRequestValidator : AbstractValidator<InboxViewRequest>
{
    public InboxViewRequestValidator()
    {
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
    }
}

