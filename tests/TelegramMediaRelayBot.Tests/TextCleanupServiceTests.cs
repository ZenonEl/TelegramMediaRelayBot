// Copyright (C) 2024-2025
using FluentAssertions;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.Tests;

public class TextCleanupServiceTests
{
    private static IOptionsMonitor<TextCleanupConfiguration> CreateOptions(TextCleanupConfiguration cfg)
    {
        return new OptionsMonitorStub(cfg);
    }

    [Fact]
    public void Cleanup_NoRules_ReturnsOriginal()
    {
        var cfg = new TextCleanupConfiguration { Enabled = true, Rules = new() };
        var svc = new TextCleanupService(CreateOptions(cfg));
        svc.Cleanup("Hello world").Should().Be("Hello world");
    }

    [Fact]
    public void Cleanup_DomainRule_AppliesForMatchingHost()
    {
        var cfg = new TextCleanupConfiguration
        {
            Enabled = true,
            Rules = new()
            {
                new TextCleanupRule
                {
                    Domains = new() { "pinterest.com" },
                    Pattern = @"^look at this[!\s:]*",
                    Replacement = ""
                }
            }
        };
        var svc = new TextCleanupService(CreateOptions(cfg));
        svc.Cleanup("Look at this: cats", "pinterest.com").Should().Be("cats");
        svc.Cleanup("Look at this: cats", "example.com").Should().Be("Look at this: cats");
    }

    private sealed class OptionsMonitorStub : IOptionsMonitor<TextCleanupConfiguration>
    {
        private TextCleanupConfiguration _cfg;
        public OptionsMonitorStub(TextCleanupConfiguration cfg) { _cfg = cfg; }
        public TextCleanupConfiguration CurrentValue => _cfg;
        public TextCleanupConfiguration Get(string? name) => _cfg;
        public IDisposable OnChange(Action<TextCleanupConfiguration, string?> listener) => new Nop();
        private sealed class Nop : IDisposable { public void Dispose() { } }
    }
}

