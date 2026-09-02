using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeCrawler.Models;

namespace YoutubeCrawler.Services;

using System;
using Polly;

public class YouTubeApiService
{
    private readonly List<YouTubeService> _youtubeServices;
    private readonly ILogger<YouTubeApiService> _logger;
    private int _currentKeyIndex = 0;
    private readonly IAsyncPolicy _quotaRetryPolicy;

    public YouTubeApiService(IOptions<CrawlerConfig> config, ILogger<YouTubeApiService> logger, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage> retryPolicy)
    {
        _logger = logger;
        _youtubeServices = new List<YouTubeService>();

        var apiKeys = config.Value.ApiKeys?.ToList() ?? new List<string>();
        if (!apiKeys.Any() && !string.IsNullOrWhiteSpace(config.Value.ApiKey))
        {
            apiKeys.Add(config.Value.ApiKey);
        }

        if (!apiKeys.Any())
        {
            _logger.LogWarning("No YouTube API keys provided in configuration.");
        }

        foreach (var key in apiKeys)
        {
            var initializer = new BaseClientService.Initializer()
            {
                ApiKey = key,
                ApplicationName = "YoutubeCrawler",
                HttpClientFactory = new PollyHttpClientFactory(retryPolicy)
            };
            _youtubeServices.Add(new YouTubeService(initializer));
        }

        int maxRetries = _youtubeServices.Count > 0 ? _youtubeServices.Count - 1 : 0;
        _quotaRetryPolicy = Policy
            .Handle<Google.GoogleApiException>(IsQuotaExceededError)
            .RetryAsync(maxRetries, onRetry: (exception, retryAttempt, context) =>
            {
                if (_youtubeServices.Count > 0)
                {
                    System.Threading.Interlocked.Increment(ref _currentKeyIndex);
                    _logger.LogWarning("Quota exceeded. Switching to next API key. Retry attempt: {RetryAttempt}", retryAttempt);
                }
            });
    }

    private YouTubeService CurrentService
    {
        get
        {
            if (_youtubeServices.Count == 0) throw new InvalidOperationException("No YouTube API services available.");
            int index = Math.Abs(_currentKeyIndex % _youtubeServices.Count);
            return _youtubeServices[index];
        }
    }

    private static bool IsQuotaExceededError(Google.GoogleApiException ex)
    {
        if (ex.Error?.Code == 403 && ex.Error.Errors != null)
        {
            return ex.Error.Errors.Any(e =>
                e.Reason.Contains("quotaExceeded", StringComparison.OrdinalIgnoreCase) ||
                e.Reason.Contains("dailyLimitExceeded", StringComparison.OrdinalIgnoreCase) ||
                e.Reason.Contains("rateLimitExceeded", StringComparison.OrdinalIgnoreCase));
        }
        return false;
    }

    public async Task<Channel?> GetChannelByIdAsync(string channelId, CancellationToken cancellationToken)
    {
        return await _quotaRetryPolicy.ExecuteAsync(async () =>
        {
            var request = CurrentService.Channels.List("snippet,statistics,brandingSettings,contentDetails");
            request.Id = channelId;
            var response = await request.ExecuteAsync(cancellationToken);
            return response.Items?.FirstOrDefault();
        });
    }

    public async Task<Channel?> GetChannelByHandleAsync(string handle, CancellationToken cancellationToken)
    {
        return await _quotaRetryPolicy.ExecuteAsync(async () =>
        {
            var request = CurrentService.Channels.List("snippet,statistics,brandingSettings,contentDetails");
            request.ForHandle = handle;
            var response = await request.ExecuteAsync(cancellationToken);
            return response.Items?.FirstOrDefault();
        });
    }

    public async Task<Channel?> GetChannelByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await _quotaRetryPolicy.ExecuteAsync(async () =>
        {
            var request = CurrentService.Channels.List("snippet,statistics,brandingSettings,contentDetails");
            request.ForUsername = username;
            var response = await request.ExecuteAsync(cancellationToken);
            return response.Items?.FirstOrDefault();
        });
    }

    public async Task<List<PlaylistItem>> GetPlaylistItemsAsync(string playlistId, CancellationToken cancellationToken)
    {
        var items = new List<PlaylistItem>();
        var nextPageToken = "";

        while (nextPageToken != null)
        {
            var response = await _quotaRetryPolicy.ExecuteAsync(async () =>
            {
                var request = CurrentService.PlaylistItems.List("snippet,contentDetails");
                request.PlaylistId = playlistId;
                request.MaxResults = 50;
                request.PageToken = nextPageToken;
                return await request.ExecuteAsync(cancellationToken);
            });

            if (response.Items != null)
            {
                items.AddRange(response.Items);
            }
            nextPageToken = response.NextPageToken;
        }

        return items;
    }

    public async Task<List<Video>> GetVideoDetailsAsync(IEnumerable<string> videoIds, CancellationToken cancellationToken)
    {
        var videos = new List<Video>();
        var idsList = videoIds.ToList();

        for (int i = 0; i < idsList.Count; i += 50)
        {
            var batch = idsList.Skip(i).Take(50).ToList();

            var response = await _quotaRetryPolicy.ExecuteAsync(async () =>
            {
                var request = CurrentService.Videos.List("snippet,contentDetails,statistics,status,paidProductPlacementDetails");
                request.Id = string.Join(",", batch);
                return await request.ExecuteAsync(cancellationToken);
            });

            if (response.Items != null)
            {
                videos.AddRange(response.Items);
            }
        }
        return videos;
    }

    public async Task<List<CommentThread>> GetCommentThreadsAsync(string videoId, CancellationToken cancellationToken)
    {
        var comments = new List<CommentThread>();
        var nextPageToken = "";

        while (nextPageToken != null)
        {
            try
            {
                var response = await _quotaRetryPolicy.ExecuteAsync(async () =>
                {
                    var request = CurrentService.CommentThreads.List("snippet,replies");
                    request.VideoId = videoId;
                    request.MaxResults = 100;
                    request.PageToken = nextPageToken;
                    request.TextFormat = CommentThreadsResource.ListRequest.TextFormatEnum.PlainText;
                    return await request.ExecuteAsync(cancellationToken);
                });

                if (response.Items != null)
                {
                    comments.AddRange(response.Items);
                }
                nextPageToken = response.NextPageToken;
            }
            catch (Google.GoogleApiException ex) when (ex.Error?.Code == 403 && !IsQuotaExceededError(ex))
            {
                // Comments might be disabled (but not quota error)
                _logger.LogWarning("Comments are disabled for video {VideoId}.", videoId);
                break;
            }
        }
        return comments;
    }

    public async Task<List<Caption>> GetCaptionsAsync(string videoId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _quotaRetryPolicy.ExecuteAsync(async () =>
            {
                var request = CurrentService.Captions.List("snippet", videoId);
                return await request.ExecuteAsync(cancellationToken);
            });
            return response.Items?.ToList() ?? new List<Caption>();
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 403 && !IsQuotaExceededError(ex))
        {
            _logger.LogWarning("Captions are disabled or private for video {VideoId}.", videoId);
            return new List<Caption>();
        }
    }
}
