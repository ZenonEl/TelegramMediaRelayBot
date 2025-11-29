// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Domain.Models;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline; 

// --- Context ---
public class DownloadContext
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string OriginalUrl { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string? ActiveProxyUrl { get; set; }
    public AuthenticationData? AuthData { get; set; }
    public List<DownloadedFile> ResultFiles { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> ExecutionLog { get; } = new();
    public void Log(string message) => ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    public Action<string>? ProgressCallback { get; set; }
}

public class AuthenticationData
{
    public string? CookieFilePath { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ApiToken { get; set; }
}

public class DownloadedFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSize => new FileInfo(FilePath).Length;
    public MediaType MediaType { get; set; } 
}

// --- ExecutionResult ---
public enum ExecutionStatus
{
    Success, FatalError, RetryableError, ContentNotSupported
}

public class ExecutionResult
{
    public ExecutionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public bool SuggestSwitchProxy { get; set; } = false;

    public static ExecutionResult Success() => new() { Status = ExecutionStatus.Success };
    public static ExecutionResult Fatal(string message, Exception? ex = null) => 
        new() { Status = ExecutionStatus.FatalError, ErrorMessage = message, Exception = ex };
    public static ExecutionResult Retryable(string message, bool switchProxy = false) => 
        new() { Status = ExecutionStatus.RetryableError, ErrorMessage = message, SuggestSwitchProxy = switchProxy };
    public static ExecutionResult NotSupported(string message) => 
        new() { Status = ExecutionStatus.ContentNotSupported, ErrorMessage = message };
}

// --- Interfaces ---
public interface IDownloadExecutor
{
    string Name { get; }
    Task<ExecutionResult> ExecuteAsync(DownloadContext context, CancellationToken ct);
}

public interface IPostProcessor
{
    string Name { get; }
    int Order { get; }
    bool CanProcess(DownloadContext context);
    Task ProcessAsync(DownloadContext context, CancellationToken ct);
}

public interface IRetryOrchestrator
{
    Task<ExecutionResult> ExecuteWithRetriesAsync(IDownloadExecutor executor, DownloadContext context, CancellationToken ct);
}