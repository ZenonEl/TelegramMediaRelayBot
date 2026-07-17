using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.Tests.Utils;

public class LinkExtractionTests
{
    [Fact]
    public void BareUrl_IsExtracted_WithEmptyCaption()
    {
        var (link, caption) = CommonUtilities.ExtractLinkAndCaption("https://pin.it/5mJ4IE28B");
        Assert.Equal("https://pin.it/5mJ4IE28B", link);
        Assert.Equal("", caption);
    }

    [Fact]
    public void ShareJunkBeforeUrl_IsDropped()
    {
        var (link, caption) = CommonUtilities.ExtractLinkAndCaption("Take a look! 📌 https://pin.it/5mJ4IE28B");
        Assert.Equal("https://pin.it/5mJ4IE28B", link);
        Assert.Equal("", caption);
    }

    [Fact]
    public void CaptionOnFollowingLine_BecomesCaption()
    {
        var (link, caption) = CommonUtilities.ExtractLinkAndCaption("https://example.com/v\nmy note");
        Assert.Equal("https://example.com/v", link);
        Assert.Equal("my note", caption);
    }

    [Fact]
    public void TrailingPunctuation_IsTrimmedFromUrl()
    {
        var (link, _) = CommonUtilities.ExtractLinkAndCaption("see (https://example.com/v).");
        Assert.Equal("https://example.com/v", link);
    }

    [Fact]
    public void NoUrl_ReturnsNull()
    {
        var (link, caption) = CommonUtilities.ExtractLinkAndCaption("just some text");
        Assert.Null(link);
        Assert.Equal("", caption);
    }
}
