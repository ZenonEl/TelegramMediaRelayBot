using FluentAssertions;
using Moq;
using TelegramMediaRelayBot.Database;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.SiteFilter;

namespace TelegramMediaRelayBot.Tests;

public class DefaultUserFilterServiceTests
{
    [Fact]
    public async Task FilterUsersByLink_ExcludesByCategoryAndDomain()
    {
        // Arrange
        var userGetter = new Mock<IUserGetter>();
        userGetter.Setup(g => g.GetUserIDbyTelegramID(1L)).Returns(100);
        userGetter.Setup(g => g.GetUserIDbyTelegramID(2L)).Returns(200);

        var privacyGetter = new Mock<IPrivacySettingsGetter>();
        privacyGetter.Setup(g => g.GetAllActiveUserRulesWithTargets(100)).ReturnsAsync(new List<PrivacyRuleResult>
        {
            new() { Type = PrivacyRuleType.SOCIAL_SITE_FILTER, Action = PrivacyRuleAction.SOCIAL_FILTER, TargetValue = "" }
        });

        privacyGetter.Setup(g => g.GetAllActiveUserRulesWithTargets(200)).ReturnsAsync(new List<PrivacyRuleResult>
        {
            new() { Type = PrivacyRuleType.SITES_BY_DOMAIN_FILTER, Action = PrivacyRuleAction.DOMAIN_FILTER, TargetValue = "example.com" }
        });

        var categorizer = new Mock<ILinkCategorizer>();
        categorizer.Setup(c => c.DetermineCategory(It.IsAny<string>())).Returns("Social");

        var service = new DefaultUserFilterService(userGetter.Object, privacyGetter.Object);
        var users = new List<long> { 1L, 2L, 3L };

        // For user 3 -> no rules (userId 0) => should pass
        userGetter.Setup(g => g.GetUserIDbyTelegramID(3L)).Returns(0);

        // Act
        var result = await service.FilterUsersByLink(users, "http://sub.example.com/path", categorizer.Object);

        // Assert: user 1 excluded by category, user 2 excluded by domain, user 3 allowed
        result.Should().BeEquivalentTo(new List<long> { 3L });
    }
}

