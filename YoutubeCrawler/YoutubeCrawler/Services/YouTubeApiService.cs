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

public class YouTubeApiService
{
    private readonly YouTubeService _youtubeService;
    private readonly ILogger<YouTubeApiService> _logger;

    public YouTubeApiService(IOptions<CrawlerConfig> config, ILogger<YouTubeApiService> logger, Polly.IAsyncPolicy<System.Net.Http.HttpResponseMessage> retryPolicy)
    {
        _logger = logger;

        var initializer = new BaseClientService.Initializer()
        {
            ApiKey = config.Value.ApiKey,
            ApplicationName = "YoutubeCrawler",
            HttpClientFactory = new PollyHttpClientFactory(retryPolicy)
        };
        _youtubeService = new YouTubeService(initializer);
    }

    public async Task<Channel?> GetChannelByIdAsync(string channelId, CancellationToken cancellationToken)
    {
        var request = _youtubeService.Channels.List("snippet,statistics,brandingSettings,contentDetails");
        request.Id = channelId;
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Items?.FirstOrDefault();
    }

    public async Task<Channel?> GetChannelByHandleAsync(string handle, CancellationToken cancellationToken)
    {
        var request = _youtubeService.Channels.List("snippet,statistics,brandingSettings,contentDetails");
        request.ForHandle = handle;
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Items?.FirstOrDefault();
    }

    public async Task<Channel?> GetChannelByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var request = _youtubeService.Channels.List("snippet,statistics,brandingSettings,contentDetails");
        request.ForUsername = username;
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Items?.FirstOrDefault();
    }

    public async Task<List<PlaylistItem>> GetPlaylistItemsAsync(string playlistId, CancellationToken cancellationToken)
    {
        var items = new List<PlaylistItem>();
        var nextPageToken = "";

        while (nextPageToken != null)
        {
            var request = _youtubeService.PlaylistItems.List("snippet,contentDetails");
            request.PlaylistId = playlistId;
            request.MaxResults = 50;
            request.PageToken = nextPageToken;

            var response = await request.ExecuteAsync(cancellationToken);
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
            var request = _youtubeService.Videos.List("snippet,contentDetails,statistics,status,paidProductPlacementDetails");
            request.Id = string.Join(",", batch);

            var response = await request.ExecuteAsync(cancellationToken);
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
            var request = _youtubeService.CommentThreads.List("snippet,replies");
            request.VideoId = videoId;
            request.MaxResults = 100;
            request.PageToken = nextPageToken;
            request.TextFormat = CommentThreadsResource.ListRequest.TextFormatEnum.PlainText;

            try
            {
                var response = await request.ExecuteAsync(cancellationToken);
                if (response.Items != null)
                {
                    comments.AddRange(response.Items);
                }
                nextPageToken = response.NextPageToken;
            }
            catch (Google.GoogleApiException ex) when (ex.Error?.Code == 403)
            {
                // Comments might be disabled
                _logger.LogWarning("Comments are disabled for video {VideoId}.", videoId);
                break;
            }
        }
        return comments;
    }

    public async Task<List<Caption>> GetCaptionsAsync(string videoId, CancellationToken cancellationToken)
    {
        var request = _youtubeService.Captions.List("snippet", videoId);
        try
        {
            var response = await request.ExecuteAsync(cancellationToken);
            return response.Items?.ToList() ?? new List<Caption>();
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 403)
        {
            _logger.LogWarning("Captions are disabled or private for video {VideoId}.", videoId);
            return new List<Caption>();
        }
    }
}
