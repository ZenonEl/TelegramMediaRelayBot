using System.Text.RegularExpressions;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Domain.Models;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Downloaders;

public abstract class BaseMediaDownloader : IMediaDownloader
{
    protected readonly IProcessRunner ProcessRunner;
    protected readonly IArgumentBuilder ArgumentBuilder;
    public DownloaderDefinition Config { get; }

    public string Name => Config.Name;
    public bool IsEnabled => Config.Enabled;
    public int Priority => Config.Priority;
    private readonly List<Regex> _urlRegexPatterns = new();
    private readonly HashSet<string> _urlHostPatterns = new();

    protected BaseMediaDownloader(
        DownloaderDefinition config,
        IProcessRunner processRunner,
        IArgumentBuilder argumentBuilder)
    {
        Config = config;
        ProcessRunner = processRunner;
        ArgumentBuilder = argumentBuilder;

        _urlRegexPatterns = new List<Regex>();
        _urlHostPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in Config.UrlMatching.Patterns)
        {
            if (pattern.Contains("\\") || pattern.Contains("^") || pattern.Contains("$"))
            {
                try 
                {
                    _urlRegexPatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                }
                catch 
                {
                    Log.Verbose("regexError");
                }
            }
            else
            {
                _urlHostPatterns.Add(pattern);
            }
        }
    }

    private bool IsMatchingPattern(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            var uri = new Uri(url);

            if (_urlHostPatterns.Contains(uri.Host))
            {
                return true;
            }

            if (_urlRegexPatterns.Any(regex => regex.IsMatch(url)))
            {
                return true;
            }
        }
        catch (UriFormatException)
        {
            return false;
        }

        return false;
    }

    public virtual bool CanHandle(string url)
    {
        if (Config.UrlMatching.Mode == UrlMatchingMode.Any) 
            return true;

        bool isMatch = IsMatchingPattern(url);

        return Config.UrlMatching.Mode switch
        {
            UrlMatchingMode.Whitelist => isMatch,
            UrlMatchingMode.Blacklist => !isMatch,
            _ => false
        };
    }

    public async Task<DownloadResult> Download(string url, DownloadOptions options, CancellationToken ct)
    {
        var tempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirPath);

        try
        {
            var cookiesPath = "";
            
            var context = new ArgumentBuilderContext
            {
                Url = url,
                OutputPath = tempDirPath,
                ProxyAddress = options.ProxyUrl,
                CookiesPath = cookiesPath,
                FormatSelection = null // TODO
            };
            
            List<string> arguments = ArgumentBuilder.Build(Config.ArgumentList, context);
            
            var processOptions = new ProcessRunOptions
            {
                FileName = Config.ExecutablePath,
                Arguments = arguments,
                Timeout = options.Timeout ?? TimeSpan.FromMinutes(5),
                OnOutputLine = options.OnProgress
            };

            Log.Information("Starting download with {Downloader} for {Url}", Name, url);
            Log.Debug("Arguments: {Arguments}", string.Join(" ", arguments));

            var commandResult = await ProcessRunner.RunAsync(processOptions, ct);
            
            Log.Debug("Process for {Downloader} finished. ExitCode={ExitCode}, TimedOut={TimedOut}", Name, commandResult.ExitCode, commandResult.TimedOut);
            
            if (commandResult.ExitCode != 0 || commandResult.TimedOut)
            {
                return new DownloadResult
                {
                    Success = false,
                    ErrorMessage = commandResult.TimedOut ? "Operation timed out." : commandResult.ErrorOutput
                };
            }

            return await ProcessSuccessResult(tempDirPath, commandResult, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Download failed for {Downloader}", Name);
            if (ex is OperationCanceledException) throw; 
            return new DownloadResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            try { Directory.Delete(tempDirPath, true); }
            catch (Exception ex) { Log.Warning(ex, "Failed to delete temp directory {TempDir}", tempDirPath); }
        }
    }

    protected abstract Task<DownloadResult> ProcessSuccessResult(string tempDirPath, CommandResult commandResult, CancellationToken ct);
}