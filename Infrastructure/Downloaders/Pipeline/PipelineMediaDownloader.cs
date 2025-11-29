using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline;

namespace TelegramMediaRelayBot.Infrastructure.Pipeline;

public class PipelineMediaDownloader : IMediaDownloader
{
    public DownloaderDefinition Config => _config;
    private readonly DownloaderDefinition _config;
    private readonly IDownloadExecutor _executor;
    private readonly IRetryOrchestrator _retryOrchestrator;
    private readonly IEnumerable<IPostProcessor> _postProcessors;
    private readonly ICredentialProvider _credentialProvider;
    private readonly IProxyPolicyManager _proxyManager;

    private readonly List<Regex> _urlRegexPatterns;
    private readonly HashSet<string> _urlHostPatterns;

    public string Name => _config.Name;
    public bool IsEnabled => _config.Enabled;
    public int Priority => _config.Priority;

    public PipelineMediaDownloader(
        DownloaderDefinition config,
        IDownloadExecutor executor,
        IRetryOrchestrator retryOrchestrator,
        IEnumerable<IPostProcessor> postProcessors,
        ICredentialProvider credentialProvider,
        IProxyPolicyManager proxyManager)
    {
        _config = config;
        _executor = executor;
        _retryOrchestrator = retryOrchestrator;
        _postProcessors = postProcessors.OrderBy(p => p.Order).ToList();
        _credentialProvider = credentialProvider;
        _proxyManager = proxyManager;

        // --- Инициализация логики матчинга URL ---
        _urlRegexPatterns = new List<Regex>();
        _urlHostPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in _config.UrlMatching.Patterns)
        {
            if (pattern.Contains("\\") || pattern.Contains("^") || pattern.Contains("$"))
            {
                try 
                {
                    _urlRegexPatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                }
                catch {}
            }
            else
            {
                _urlHostPatterns.Add(pattern);
            }
        }
    }

    public bool CanHandle(string url)
    {
        if (_config.UrlMatching.Mode == UrlMatchingMode.Any) 
            return true;

        bool isMatch = IsMatchingPattern(url);

        return _config.UrlMatching.Mode switch
        {
            UrlMatchingMode.Whitelist => isMatch,  // Whitelist
            UrlMatchingMode.Blacklist => !isMatch, // Blacklist
            _ => false
        };
    }

    private bool IsMatchingPattern(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        try
        {
            var uri = new Uri(url);
            if (_urlHostPatterns.Contains(uri.Host)) return true;
            if (_urlRegexPatterns.Any(regex => regex.IsMatch(url))) return true;
        }
        catch { return false; }
        return false;
    }

public async Task<DownloadResult> Download(string url, DownloadOptions options, CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        var context = new DownloadContext
        {
            OriginalUrl = url,
            OutputDirectory = tempPath,
            ActiveProxyUrl = _proxyManager.GetProxyAddress(_config, url) ?? options.ProxyUrl,
            ProgressCallback = options.OnProgress,
            AuthData = new AuthenticationData
            {
                CookieFilePath = _credentialProvider.GetCookieFilePath(_config.Authentication.CookieFile),
                Username = _credentialProvider.ResolveSecret(_config.Authentication.Username),
                Password = _credentialProvider.ResolveSecret(_config.Authentication.Password)
            }
        };

        try
        {
            // 1. ЗАПУСК ЗАГРУЗКИ
            Log.Debug("--- PIPELINE START: {Url} ---", url);
            var executionResult = await _retryOrchestrator.ExecuteWithRetriesAsync(_executor, context, ct);

            if (executionResult.Status != ExecutionStatus.Success)
            {
                return new DownloadResult 
                { 
                    Success = false, 
                    ErrorMessage = executionResult.ErrorMessage ?? "Pipeline execution failed" 
                };
            }

            // ЛОГ ПОСЛЕ СКАЧИВАНИЯ
            LogFilesState("After Download", context);

            // 2. ЗАПУСК ПОСТ-ПРОЦЕССОРОВ
            foreach (var processor in _postProcessors)
            {
                if (processor.CanProcess(context))
                {
                    Log.Debug(">>> Running Processor: {Name}", processor.Name);
                    await processor.ProcessAsync(context, ct);
                    
                    // ЛОГ ПОСЛЕ КАЖДОГО ПРОЦЕССОРА
                    LogFilesState($"After {processor.Name}", context);
                }
                else
                {
                    Log.Debug(">>> Skipping Processor: {Name} (Condition not met)", processor.Name);
                }
            }

            // 3. ФИНАЛЬНАЯ ПОДГОТОВКА
            var result = new DownloadResult
            {
                Success = true,
                MediaFiles = new List<byte[]>(),
                MediaType = context.ResultFiles.FirstOrDefault()?.MediaType ?? MediaType.None,
            };

            foreach (var file in context.ResultFiles)
            {
                if (System.IO.File.Exists(file.FilePath))
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(file.FilePath, ct);
                    result.MediaFiles.Add(bytes);
                    if (bytes.Length > 52428800) 
                    {
                        Log.Error("!!! DANGER !!! File {Name} is {Size:F2} MB, which is OVER Telegram limit (50MB)!", 
                            file.FileName, bytes.Length / 1024.0 / 1024.0);
                    }
                }
            }
            
            Log.Debug("--- PIPELINE FINISHED ---");
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Pipeline crashed");
            return new DownloadResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            try { Directory.Delete(tempPath, true); } catch { }
        }
    }

    // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ЛОГОВ ---
    private void LogFilesState(string stage, DownloadContext context)
    {
        var files = context.ResultFiles;
        Log.Information("📊 [{Stage}] Files count: {Count}", stage, files.Count);
        
        foreach (var file in files)
        {
            long size = 0;
            if (System.IO.File.Exists(file.FilePath))
            {
                size = new FileInfo(file.FilePath).Length;
            }
            else
            {
                size = file.FileSize; 
            }

            double sizeMb = size / 1024.0 / 1024.0;
            string icon = sizeMb > 49.0 ? "🔴" : "🟢";

            Log.Information("   {Icon} {Name}: {Size:F2} MB ({MediaType})", icon, file.FileName, sizeMb, file.MediaType);
        }
    }
}