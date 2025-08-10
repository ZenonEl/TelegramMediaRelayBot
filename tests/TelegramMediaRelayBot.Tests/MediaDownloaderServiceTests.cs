using FluentAssertions;
using Moq;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot;
using TelegramMediaRelayBot.Config;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using Telegram.Bot;

namespace TelegramMediaRelayBot.Tests;

public class MediaDownloaderServiceTests
{
    [Fact]
    public async Task DownloadMediaWithFallback_UsesNextDownloader_WhenFirstFails()
    {
        // Arrange
        var botConfig = new BotConfiguration { Proxy = "http://proxy:8080" };
        var optionsMonitor = new Mock<IOptionsMonitor<BotConfiguration>>();
        optionsMonitor.SetupGet(o => o.CurrentValue).Returns(botConfig);

        var failing = new Mock<IMediaDownloader>();
        failing.SetupGet(d => d.Name).Returns("fail");
        failing.Setup(d => d.CanHandle(It.IsAny<string>())).Returns(true);
        failing.SetupGet(d => d.IsEnabled).Returns(true);
        failing.SetupGet(d => d.Priority).Returns(10);
        failing.Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<DownloadOptions>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new DownloadResult { Success = false, ErrorMessage = "oops" });

        var succeeding = new Mock<IMediaDownloader>();
        succeeding.SetupGet(d => d.Name).Returns("ok");
        succeeding.Setup(d => d.CanHandle(It.IsAny<string>())).Returns(true);
        succeeding.SetupGet(d => d.IsEnabled).Returns(true);
        succeeding.SetupGet(d => d.Priority).Returns(5);
        var payload = new List<byte[]> { new byte[] { 1, 2, 3 } };
        succeeding.Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<DownloadOptions>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new DownloadResult { Success = true, MediaFiles = payload });

        var factory = new Mock<IMediaDownloaderFactory>();
        factory.Setup(f => f.GetDownloadersForUrl(It.IsAny<string>()))
               .Returns(new[] { failing.Object, succeeding.Object });

        var downloading = new Mock<IOptionsMonitor<DownloadingConfiguration>>();
        downloading.SetupGet(d => d.CurrentValue).Returns(new DownloadingConfiguration());
        var service = new MediaDownloaderService(factory.Object, optionsMonitor.Object, downloading.Object);

        // Act
        var result = await service.DownloadMediaWithFallback(new Mock<ITelegramBotClient>().Object, "http://example", default!, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
        failing.Verify(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<DownloadOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        succeeding.Verify(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<DownloadOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadMediaWithFallback_ReturnsNull_WhenNoDownloaders()
    {
        var optionsMonitor = new Mock<IOptionsMonitor<BotConfiguration>>();
        optionsMonitor.SetupGet(o => o.CurrentValue).Returns(new BotConfiguration());

        var factory = new Mock<IMediaDownloaderFactory>();
        factory.Setup(f => f.GetDownloadersForUrl(It.IsAny<string>()))
               .Returns(Array.Empty<IMediaDownloader>());

        var downloading = new Mock<IOptionsMonitor<DownloadingConfiguration>>();
        downloading.SetupGet(d => d.CurrentValue).Returns(new DownloadingConfiguration());
        var service = new MediaDownloaderService(factory.Object, optionsMonitor.Object, downloading.Object);

        var result = await service.DownloadMediaWithFallback(new Mock<ITelegramBotClient>().Object, "http://none", default!, CancellationToken.None);

        result.Should().BeNull();
    }
}

