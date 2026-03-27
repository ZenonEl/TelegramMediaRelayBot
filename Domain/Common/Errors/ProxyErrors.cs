namespace TelegramMediaRelayBot.Domain.Common.Errors;

public static class ProxyErrors
{
    public static Error ConnectionFailed(string proxyUrl) =>
        Error.Infrastructure("Proxy.ConnectionFailed", $"Failed to connect through proxy: {proxyUrl}");

    public static Error TorCircuitFailed() =>
        Error.Infrastructure("Proxy.TorCircuitFailed", "Failed to request new Tor circuit");
}
