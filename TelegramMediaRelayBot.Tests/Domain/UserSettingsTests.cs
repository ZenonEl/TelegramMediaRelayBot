using TelegramMediaRelayBot.Domain.Models;

namespace TelegramMediaRelayBot.Tests.Domain;

public class UserSettingsTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveValues()
    {
        var settings = new UserSettings();
        settings.Distribution.DefaultAction = "send_to_all";
        settings.Distribution.AutoSendDelaySeconds = 60;
        settings.Privacy.InboxEnabled = true;

        var json = settings.ToJson();
        var restored = UserSettings.FromJson(json);

        Assert.Equal("send_to_all", restored.Distribution.DefaultAction);
        Assert.Equal(60, restored.Distribution.AutoSendDelaySeconds);
        Assert.True(restored.Privacy.InboxEnabled);
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var settings = new UserSettings();

        Assert.Equal("send_only_to_me", settings.Distribution.DefaultAction);
        Assert.Equal("", settings.Distribution.DefaultActionCondition);
        Assert.Equal(30, settings.Distribution.AutoSendDelaySeconds);
        Assert.Empty(settings.Distribution.TargetGroupIds);
        Assert.Empty(settings.Distribution.TargetContactIds);
        Assert.False(settings.Privacy.InboxEnabled);
        Assert.True(settings.Privacy.AllowContentForwarding);
        Assert.Equal("everyone", settings.Privacy.WhoCanFindMe);
    }

    [Fact]
    public void FromJson_EmptyString_ShouldReturnDefaults()
    {
        var settings = UserSettings.FromJson("");

        Assert.Equal("send_only_to_me", settings.Distribution.DefaultAction);
        Assert.Equal(30, settings.Distribution.AutoSendDelaySeconds);
    }

    [Fact]
    public void FromJson_Null_ShouldReturnDefaults()
    {
        var settings = UserSettings.FromJson(null!);

        Assert.NotNull(settings);
        Assert.NotNull(settings.Distribution);
        Assert.NotNull(settings.Privacy);
    }

    [Fact]
    public void FromJson_InvalidJson_ShouldReturnDefaults()
    {
        var settings = UserSettings.FromJson("{broken json!!!");

        Assert.NotNull(settings);
        Assert.Equal("send_only_to_me", settings.Distribution.DefaultAction);
    }

    [Fact]
    public void NestedSiteFilter_ShouldRoundTrip()
    {
        var settings = new UserSettings();
        settings.Privacy.SiteFilter.Enabled = true;
        settings.Privacy.SiteFilter.FilterType = "blocklist";
        settings.Privacy.SiteFilter.BlockedDomains.Add("example.com");

        var json = settings.ToJson();
        var restored = UserSettings.FromJson(json);

        Assert.True(restored.Privacy.SiteFilter.Enabled);
        Assert.Equal("blocklist", restored.Privacy.SiteFilter.FilterType);
        Assert.Single(restored.Privacy.SiteFilter.BlockedDomains);
        Assert.Equal("example.com", restored.Privacy.SiteFilter.BlockedDomains[0]);
    }

    [Fact]
    public void DistributionTargets_ShouldRoundTrip()
    {
        var settings = new UserSettings();
        settings.Distribution.TargetGroupIds.AddRange([1, 2, 3]);
        settings.Distribution.TargetContactIds.AddRange([10, 20]);

        var json = settings.ToJson();
        var restored = UserSettings.FromJson(json);

        Assert.Equal([1, 2, 3], restored.Distribution.TargetGroupIds);
        Assert.Equal([10, 20], restored.Distribution.TargetContactIds);
    }

    [Fact]
    public void SiteFilterDefaults_ShouldBeCorrect()
    {
        var filter = new SiteFilterSettings();

        Assert.False(filter.Enabled);
        Assert.Equal("none", filter.FilterType);
        Assert.Empty(filter.BlockedDomains);
    }
}
