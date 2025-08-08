using FluentAssertions;
using Moq;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Infrastructure.Factories;

namespace TelegramMediaRelayBot.Tests;

public class MediaDownloaderFactoryTests
{
    [Fact]
    public void GetDownloader_PicksHighestPriorityEnabledThatCanHandle()
    {
        var m1 = new Mock<IMediaDownloader>();
        m1.SetupGet(d => d.IsEnabled).Returns(true);
        m1.SetupGet(d => d.Priority).Returns(1);
        m1.Setup(d => d.CanHandle(It.IsAny<string>())).Returns(true);
        m1.SetupGet(d => d.Name).Returns("d1");

        var m2 = new Mock<IMediaDownloader>();
        m2.SetupGet(d => d.IsEnabled).Returns(true);
        m2.SetupGet(d => d.Priority).Returns(5);
        m2.Setup(d => d.CanHandle(It.IsAny<string>())).Returns(true);
        m2.SetupGet(d => d.Name).Returns("d2");

        var factory = new MediaDownloaderFactory(new[] { m1.Object, m2.Object });

        var result = factory.GetDownloader("https://example.com");

        result.Should().BeSameAs(m2.Object);
    }

    [Fact]
    public void GetDownloader_Throws_WhenNoEnabledDownloaderCanHandle()
    {
        var m1 = new Mock<IMediaDownloader>();
        m1.SetupGet(d => d.IsEnabled).Returns(false);
        m1.Setup(d => d.CanHandle(It.IsAny<string>())).Returns(false);

        var factory = new MediaDownloaderFactory(new[] { m1.Object });

        var act = () => factory.GetDownloader("https://nope");

        act.Should().Throw<InvalidOperationException>();
    }
}

