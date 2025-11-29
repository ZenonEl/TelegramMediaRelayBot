// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Microsoft.Extensions.Options;
using TelegramMediaRelayBot.Config.Downloaders;
using TelegramMediaRelayBot.Domain.Interfaces;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Arguments;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Pipeline;
using TelegramMediaRelayBot.Infrastructure.Downloaders.Policies;
using TelegramMediaRelayBot.Infrastructure.Pipeline;
using TelegramMediaRelayBot.Infrastructure.Pipeline.Executors;
using TelegramMediaRelayBot.Infrastructure.Processes;

namespace TelegramMediaRelayBot.Infrastructure.Factories;

public class MediaDownloaderFactory : IMediaDownloaderFactory
{
    private DownloaderConfigRoot _config;
    private readonly IProcessRunner _processRunner;
    private readonly IArgumentBuilder _argumentBuilder;
    private readonly IRetryOrchestrator _retryOrchestrator;
    private readonly ICredentialProvider _credentialProvider;
    private readonly IProxyPolicyManager _proxyManager;
    private readonly IEnumerable<IPostProcessor> _allPostProcessors;

    private readonly List<IMediaDownloader> _downloaders;

    public MediaDownloaderFactory(
        IOptionsMonitor<DownloaderConfigRoot> configMonitor,
        IProcessRunner processRunner,
        IArgumentBuilder argumentBuilder,
        IRetryOrchestrator retryOrchestrator,
        ICredentialProvider credentialProvider,
        IProxyPolicyManager proxyManager,
        IEnumerable<IPostProcessor> allPostProcessors)
    {
        _config = configMonitor.CurrentValue;
        
        _processRunner = processRunner;
        _argumentBuilder = argumentBuilder;
        _retryOrchestrator = retryOrchestrator;
        _credentialProvider = credentialProvider;
        _proxyManager = proxyManager;
        _allPostProcessors = allPostProcessors;

        _downloaders = new List<IMediaDownloader>();
        InitializeDownloaders();

        configMonitor.OnChange(newConfig =>
        {
            Log.Information("Configuration changed. Rebuilding pipeline...");
            lock (_downloaders)
            {
                _config = newConfig;
                _downloaders.Clear();
                InitializeDownloaders();
            }
        });
    }

    private void InitializeDownloaders()
    {
        foreach (var def in _config.Downloaders)
        {
            LoadPatternsFromFile(def.UrlMatching);

            IDownloadExecutor executor = new CliDownloadExecutor(def, _processRunner, _argumentBuilder);

            var selectedProcessors = _allPostProcessors
                .Where(p => def.PostProcessors.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var pipeline = new PipelineMediaDownloader(
                def,
                executor,
                _retryOrchestrator,
                selectedProcessors,
                _credentialProvider,
                _proxyManager
            );

            if (def.Enabled)
            {
                _downloaders.Add(pipeline);
            }
        }
    }

    private void LoadPatternsFromFile(UrlMatchingConfig urlMatchingConfig)
    {
        if (string.IsNullOrWhiteSpace(urlMatchingConfig.PatternsFile)) return;
        
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, urlMatchingConfig.PatternsFile);
            if (System.IO.File.Exists(path))
            {
                var patternsFromFile = System.IO.File.ReadAllLines(path)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'))
                    .ToList();
                
                urlMatchingConfig.Patterns.AddRange(patternsFromFile);
                Log.Debug("Loaded {Count} URL patterns from file: {File}", patternsFromFile.Count, path);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load URL patterns from file: {File}", urlMatchingConfig.PatternsFile);
        }
    }

    public IMediaDownloader GetDownloader(string url)
    {
        var downloader = _downloaders
            .Where(d => d.CanHandle(url))
            .OrderByDescending(d => d.Priority)
            .FirstOrDefault();
            
        if (downloader == null)
        {
            throw new InvalidOperationException($"No enabled downloader found for URL: {url}");
        }
        
        Log.Information("Selected downloader {Downloader} for {Url}", downloader.Name, url);
        return downloader;
    }

    public IEnumerable<IMediaDownloader> GetDownloadersForUrl(string url)
    {
        return _downloaders
            .Where(d => d.CanHandle(url))
            .OrderByDescending(d => d.Priority);
    }
    
    public IEnumerable<IMediaDownloader> GetAllDownloaders() => _downloaders;
    public IEnumerable<IMediaDownloader> GetEnabledDownloaders() => _downloaders;
}