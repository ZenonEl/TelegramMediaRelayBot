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
    
    protected BaseMediaDownloader(
        DownloaderDefinition config,
        IProcessRunner processRunner,
        IArgumentBuilder argumentBuilder)
    {
        Config = config;
        ProcessRunner = processRunner;
        ArgumentBuilder = argumentBuilder;
    }

    public virtual bool CanHandle(string url)
    {
        if (Config.UrlMatching.Mode == UrlMatchingMode.Any) return true;
        return Config.UrlMatching.Patterns.Any(p => Regex.IsMatch(url, p, RegexOptions.IgnoreCase));
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