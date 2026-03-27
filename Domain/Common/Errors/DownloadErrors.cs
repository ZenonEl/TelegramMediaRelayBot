namespace TelegramMediaRelayBot.Domain.Common.Errors;

public static class DownloadErrors
{
    public static Error UnsupportedUrl(string url) =>
        Error.Validation("Download.UnsupportedUrl", $"URL is not supported: {url}");

    public static Error NoFormatsFound(string url) =>
        Error.External("Download.NoFormats", $"No downloadable formats found for: {url}");

    public static Error Forbidden(string url) =>
        Error.External("Download.Forbidden", $"Access denied (403) for: {url}");

    public static Error RateLimited(string url) =>
        Error.External("Download.RateLimited", $"Rate limited (429) for: {url}");

    public static Error GeoBlocked(string url) =>
        Error.External("Download.GeoBlocked", $"Content is geo-blocked: {url}");

    public static Error AuthRequired(string url) =>
        Error.External("Download.AuthRequired", $"Authentication required for: {url}");

    public static Error Timeout(string url, TimeSpan duration) =>
        Error.Infrastructure("Download.Timeout", $"Download timed out after {duration.TotalSeconds}s: {url}");

    public static Error ProcessFailed(string message) =>
        Error.Infrastructure("Download.ProcessFailed", message);

    public static Error DiskFull() =>
        Error.Infrastructure("Download.DiskFull", "Insufficient disk space for download");
}
