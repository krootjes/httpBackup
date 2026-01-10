using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using httpBackupCore;

namespace httpBackupWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private static readonly HttpClient _http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("httpBackup/1.0");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("httpBackupWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            AppConfig cfg;
            try
            {
                cfg = ConfigStore.LoadOrCreateDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load config.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            var intervalMinutes = Math.Max(1, cfg.IntervalMinutes);
            await RunBackupOnce(cfg, stoppingToken);

            _logger.LogInformation("Next run in {Minutes} minutes.", intervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task RunBackupOnce(AppConfig cfg, CancellationToken ct)
    {
        if (cfg.Sites is null || cfg.Sites.Count == 0)
        {
            _logger.LogWarning("No sites configured.");
            return;
        }

        var root = (cfg.BackupFolder ?? "").Trim();
        if (string.IsNullOrWhiteSpace(root))
        {
            _logger.LogError("BackupFolder is empty in config.");
            return;
        }

        Directory.CreateDirectory(root);

        foreach (var site in cfg.Sites)
        {
            if (ct.IsCancellationRequested) return;
            if (site is null || !site.Enabled) continue;

            var prefix = (site.Name ?? "").Trim();
            var url = (site.Url ?? "").Trim();

            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Skipping site with empty Name/Url.");
                continue;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                _logger.LogWarning("Skipping invalid URL: {Url}", url);
                continue;
            }

            var safePrefix = MakeSafeFileName(prefix);

            // Per-site folder: <BackupFolder>\<prefix>\
            var siteFolder = Path.Combine(root, safePrefix);
            Directory.CreateDirectory(siteFolder);

            // Output filename:
            // backup_{prefix}_DD-MM-YYYY_HH-MM-ss.zip
            var ts = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
            var outPath = Path.Combine(siteFolder, $"backup_{safePrefix}_{ts}.zip");

            try
            {
                _logger.LogInformation("Downloading ZIP for {Prefix} from {Url}", prefix, url);

                using var resp = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Download skipped for {Prefix}: HTTP {Status} ({Url})",
                        prefix, (int)resp.StatusCode, url);
                    continue;
                }

                await using var fs = new FileStream(outPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await resp.Content.CopyToAsync(fs, ct);

                _logger.LogInformation("Saved: {File}", outPath);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // service/app is stopping
                _logger.LogInformation("Cancellation requested; stopping current run.");
                return;
            }
            catch (HttpRequestException ex)
            {
                // connection refused, DNS fail, timeout, etc. -> no stacktrace spam
                _logger.LogWarning("Download skipped for {Prefix}: {Message} ({Url})", prefix, ex.Message, url);
            }
            catch (IOException ex)
            {
                // disk / file access issues -> keep it readable
                _logger.LogWarning("IO issue for {Prefix}: {Message} ({Path})", prefix, ex.Message, outPath);
            }
            catch (Exception ex)
            {
                // unexpected -> keep stacktrace
                _logger.LogError(ex, "Unexpected error for {Prefix} ({Url})", prefix, url);
            }
        }
    }

    private static string MakeSafeFileName(string input)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');
        return input;
    }
}