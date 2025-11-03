using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface ITextCleanupService
{
    string Cleanup(string text);
}

public class TextCleanupService : ITextCleanupService
{
    private readonly List<Regex> _cleanupRegexes = new();

    public TextCleanupService(IOptionsMonitor<TextCleanupConfig> configMonitor)
    {
        LoadPatterns(configMonitor.CurrentValue);
        configMonitor.OnChange(LoadPatterns);
    }

    private void LoadPatterns(TextCleanupConfig config)
    {
        lock (_cleanupRegexes)
        {
            _cleanupRegexes.Clear();
            var allPatterns = new List<string>(config.Patterns);

            if (!string.IsNullOrWhiteSpace(config.PatternsFile))
            {
                var path = Path.Combine(AppContext.BaseDirectory, config.PatternsFile);
                if (System.IO.File.Exists(path))
                {
                    var patternsFromFile = System.IO.File.ReadAllLines(path)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'));
                    allPatterns.AddRange(patternsFromFile);
                }
            }

            foreach (var pattern in allPatterns)
            {
                try { _cleanupRegexes.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)); }
                catch (Exception ex) { Log.Warning(ex, "Invalid text cleanup regex pattern: {Pattern}", pattern); }
            }
            Log.Information("Loaded {Count} text cleanup patterns.", _cleanupRegexes.Count);
        }
    }

    public string Cleanup(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        string cleanedText = text;
        lock (_cleanupRegexes)
        {
            foreach (var regex in _cleanupRegexes)
            {
                cleanedText = regex.Replace(cleanedText, string.Empty);
            }
        }
        return cleanedText.Trim();
    }
}