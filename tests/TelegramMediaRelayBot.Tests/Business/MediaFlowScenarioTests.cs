using FluentAssertions;
using Moq;
using TelegramMediaRelayBot;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using Telegram.Bot;

namespace TelegramMediaRelayBot.Tests.Business;

public class MediaFlowScenarioTests
{
    [Fact]
    public async Task When_First_Downloader_Fails_Second_Succeeds_Files_Are_Returned()
    {
        var factory = new Mock<IMediaDownloaderFactory>();
        var d1 = new Mock<IMediaDownloader>();
        var d2 = new Mock<IMediaDownloader>();

        d1.SetupGet(x => x.IsEnabled).Returns(true);
        d1.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(true);
        d1.SetupGet(x => x.Priority).Returns(10);
        d1.SetupGet(x => x.Name).Returns("d1");
        d1.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<DownloadOptions>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new DownloadResult { Success = false, ErrorMessage = "fail" });

        d2.SetupGet(x => x.IsEnabled).Returns(true);
        d2.Setup(x => x.CanHandle(It.IsAny<string>())).Returns(true);
        d2.SetupGet(x => x.Priority).Returns(5);
        d2.SetupGet(x => x.Name).Returns("d2");
        d2.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<DownloadOptions>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new DownloadResult { Success = true, MediaFiles = new List<byte[]> { new byte[]{1} } });

        factory.Setup(f => f.GetDownloadersForUrl(It.IsAny<string>()))
               .Returns(new[] { d1.Object, d2.Object });

        var opt = Options.Create(new BotConfiguration());
        var optMon = new Mock<IOptionsMonitor<BotConfiguration>>();
        optMon.SetupGet(x => x.CurrentValue).Returns(opt.Value);
        var downloading = new Mock<IOptionsMonitor<DownloadingConfiguration>>();
        downloading.SetupGet(d => d.CurrentValue).Returns(new DownloadingConfiguration());

        var service = new MediaDownloaderService(factory.Object, optMon.Object, downloading.Object);
        var result = await service.DownloadMediaWithFallback(new Mock<ITelegramBotClient>().Object, "http://test", default!, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
    }
}

