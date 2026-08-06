namespace YoutubeCrawler.Models;

public class CrawlerConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string LogPath { get; set; } = string.Empty;
    public int MaxRetryCount { get; set; } = 5;
    public int SyncIntervalSeconds { get; set; } = 86400;
    public int BatchSize { get; set; } = 50;
}
