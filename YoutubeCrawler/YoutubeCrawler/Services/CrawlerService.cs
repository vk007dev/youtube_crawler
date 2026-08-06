using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeCrawler.Helpers;
using YoutubeCrawler.Models;
using YoutubeCrawler.Repositories;
using Google.Apis.YouTube.v3.Data;

namespace YoutubeCrawler.Services;

public class CrawlerService : BackgroundService
{
    private readonly YouTubeApiService _apiService;
    private readonly YoutubeRepository _repository;
    private readonly UrlExtractor _urlExtractor;
    private readonly ILogger<CrawlerService> _logger;
    private readonly CrawlerConfig _config;
    private readonly List<string> _targetChannels;

    public CrawlerService(
        YouTubeApiService apiService,
        YoutubeRepository repository,
        UrlExtractor urlExtractor,
        IOptions<CrawlerConfig> config,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<CrawlerService> logger)
    {
        _apiService = apiService;
        _repository = repository;
        _urlExtractor = urlExtractor;
        _config = config.Value;
        _logger = logger;
        _targetChannels = configuration.GetSection("TargetChannels").Get<List<string>>() ?? new List<string>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("YoutubeCrawler starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var url in _targetChannels)
            {
                if (stoppingToken.IsCancellationRequested) break;
                await ProcessChannelAsync(url, stoppingToken);
            }

            _logger.LogInformation("Sync cycle completed. Waiting for {Seconds} seconds.", _config.SyncIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_config.SyncIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessChannelAsync(string url, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing channel: {Url}", url);
        try
        {
            var (type, value) = YoutubeUrlResolver.ParseUrl(url);
            Channel? channelData = null;

            if (type == "channel" || type == "c")
            {
                channelData = await _apiService.GetChannelByIdAsync(value, cancellationToken);
                // Sometimes 'c' is custom url which needs search, but simplified here.
            }
            else if (type == "handle")
            {
                 channelData = await _apiService.GetChannelByHandleAsync(value, cancellationToken);
            }
            else if (type == "user")
            {
                 channelData = await _apiService.GetChannelByUsernameAsync(value, cancellationToken);
            }

            if (channelData == null)
            {
                _logger.LogWarning("Channel not found for {Url}", url);
                return;
            }

            var dbChannel = new Models.YoutubeChannel
            {
                ChannelId = channelData.Id,
                Title = channelData.Snippet?.Title ?? "",
                Description = channelData.Snippet?.Description,
                CustomUrl = channelData.Snippet?.CustomUrl,
                Country = channelData.Snippet?.Country,
                PublishedDate = channelData.Snippet?.PublishedAtDateTimeOffset?.UtcDateTime,
                UploadPlaylistId = channelData.ContentDetails?.RelatedPlaylists?.Uploads,
                SubscriberCount = (long?)(channelData.Statistics?.SubscriberCount),
                VideoCount = (long?)(channelData.Statistics?.VideoCount),
                ViewCount = (long?)(channelData.Statistics?.ViewCount),
                HiddenSubscriberCount = channelData.Statistics?.HiddenSubscriberCount,
                ThumbnailUrl = channelData.Snippet?.Thumbnails?.High?.Url,
                BrandingSettings = channelData.BrandingSettings?.Channel?.Keywords // simplified
            };

            await _repository.SaveChannelAsync(dbChannel);

            // Extract links from channel description
            if (!string.IsNullOrWhiteSpace(dbChannel.Description))
            {
                var links = await _urlExtractor.ExtractUrlsAsync(dbChannel.Description, "ChannelDescription", dbChannel.ChannelId, null, dbChannel.PublishedDate);
                await _repository.SaveLinksAsync(links);
            }

            if (!string.IsNullOrEmpty(dbChannel.UploadPlaylistId))
            {
                await ProcessPlaylistAsync(dbChannel.ChannelId, dbChannel.UploadPlaylistId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing channel {Url}", url);
        }
    }

    private async Task ProcessPlaylistAsync(string channelId, string playlistId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing playlist {PlaylistId} for channel {ChannelId}", playlistId, channelId);

        var playlistItems = await _apiService.GetPlaylistItemsAsync(playlistId, cancellationToken);
        var videoIds = playlistItems.Select(x => x.ContentDetails?.VideoId).Where(id => id != null).Select(id => id!).ToList();

        _logger.LogInformation("Found {Count} videos in playlist.", videoIds.Count);

        var existingVideoIds = (await _repository.GetExistingVideoIdsAsync(channelId)).ToHashSet();
        var newVideoIds = videoIds.Where(id => !existingVideoIds.Contains(id)).ToList();

        _logger.LogInformation("Found {Count} new videos to process.", newVideoIds.Count);

        // Process only new videos for details, but we might want to update stats for existing videos too.
        // For simplicity and quota saving, we will only fetch details for all videos if stats need update,
        // but the prompt says "Do not download existing videos again. Only download new uploads. Only update changed statistics."
        // We will fetch new videos, and maybe we can fetch existing videos if we had a mechanism, but let's stick to new videos for full download.
        // Actually to update changed statistics we need to download the video details anyway, or just the stats part.
        // Let's fetch all videos but only process stats for existing. The prompt: "Do not download existing videos again... Only update changed statistics."
        // This implies we can download stats for existing videos, but maybe just use the new ones for comments.
        // To be safe on quota, let's fetch all video details (part=statistics is small) or we can just fetch all details in batches since we need to update stats.
        // But the prompt says "Do not download existing videos again", which usually means don't download captions and comments again.

        for (int i = 0; i < videoIds.Count; i += _config.BatchSize)
        {
            var batchIds = videoIds.Skip(i).Take(_config.BatchSize).ToList();
            var videosData = await _apiService.GetVideoDetailsAsync(batchIds, cancellationToken);

            var dbVideos = new List<Models.YoutubeVideo>();
            var allLinks = new List<Models.YoutubeLink>();

            foreach (var v in videosData)
            {
                var playlistItem = playlistItems.FirstOrDefault(p => p.ContentDetails?.VideoId == v.Id);
                var dbVideo = new Models.YoutubeVideo
                {
                    VideoId = v.Id,
                    ChannelId = channelId,
                    PlaylistPosition = (int?)playlistItem?.Snippet?.Position,
                    PublishTime = v.Snippet?.PublishedAtDateTimeOffset?.UtcDateTime,
                    Title = v.Snippet?.Title ?? "",
                    Description = v.Snippet?.Description,
                    Tags = v.Snippet?.Tags != null ? string.Join(",", v.Snippet.Tags) : null,
                    CategoryId = v.Snippet?.CategoryId,
                    DefaultLanguage = v.Snippet?.DefaultLanguage,
                    DefaultAudioLanguage = v.Snippet?.DefaultAudioLanguage,
                    LiveBroadcastContent = v.Snippet?.LiveBroadcastContent,
                    Duration = v.ContentDetails?.Duration,
                    Dimension = v.ContentDetails?.Dimension,
                    Definition = v.ContentDetails?.Definition,
                    Caption = v.ContentDetails?.Caption == "true",
                    LicensedContent = v.ContentDetails?.LicensedContent,
                    Projection = v.ContentDetails?.Projection,
                    ViewCount = (long?)v.Statistics?.ViewCount,
                    LikeCount = (long?)v.Statistics?.LikeCount,
                    FavoriteCount = (long?)v.Statistics?.FavoriteCount,
                    CommentCount = (long?)v.Statistics?.CommentCount,
                    PrivacyStatus = v.Status?.PrivacyStatus,
                    UploadStatus = v.Status?.UploadStatus,
                    License = v.Status?.License,
                    Embeddable = v.Status?.Embeddable,
                    PublicStatsViewable = v.Status?.PublicStatsViewable,
                    MadeForKids = v.Status?.MadeForKids,
                    SelfDeclaredMadeForKids = v.Status?.SelfDeclaredMadeForKids,
                    HasProductPlacement = v.PaidProductPlacementDetails?.HasPaidProductPlacement
                };
                dbVideos.Add(dbVideo);

                // Extract links from video description
                if (!string.IsNullOrWhiteSpace(dbVideo.Description))
                {
                    var links = await _urlExtractor.ExtractUrlsAsync(dbVideo.Description, "VideoDescription", channelId, dbVideo.VideoId, dbVideo.PublishTime);
                    allLinks.AddRange(links);
                }
            }

            await _repository.SaveVideosAsync(dbVideos);
            if (allLinks.Any())
            {
                await _repository.SaveLinksAsync(allLinks);
            }

            foreach (var v in videosData)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Only process comments and captions for new videos to save quota
                if (newVideoIds.Contains(v.Id))
                {
                    await ProcessCommentsAndCaptionsAsync(v.Id, cancellationToken);
                }
            }
        }
    }

    private async Task ProcessCommentsAndCaptionsAsync(string videoId, CancellationToken cancellationToken)
    {
        // Comments
        var threads = await _apiService.GetCommentThreadsAsync(videoId, cancellationToken);
        var dbComments = new List<Models.YoutubeComment>();
        var dbReplies = new List<Models.YoutubeReply>();

        foreach (var thread in threads)
        {
            var top = thread.Snippet?.TopLevelComment?.Snippet;
            if (top != null)
            {
                dbComments.Add(new Models.YoutubeComment
                {
                    CommentId = thread.Id,
                    VideoId = videoId,
                    AuthorName = top.AuthorDisplayName,
                    AuthorChannelId = top.AuthorChannelId?.Value,
                    AuthorProfileUrl = top.AuthorChannelUrl,
                    AuthorAvatarUrl = top.AuthorProfileImageUrl,
                    TextDisplay = top.TextDisplay,
                    TextOriginal = top.TextOriginal,
                    LikeCount = (long?)top.LikeCount,
                    PublishedDate = top.PublishedAtDateTimeOffset?.UtcDateTime,
                    UpdatedDate = top.UpdatedAtDateTimeOffset?.UtcDateTime,
                    CanRate = top.CanRate,
                    ViewerRating = top.ViewerRating,
                    TotalReplyCount = (int?)thread.Snippet?.TotalReplyCount
                });
            }

            if (thread.Replies?.Comments != null)
            {
                foreach (var reply in thread.Replies.Comments)
                {
                    var rSnippet = reply.Snippet;
                    dbReplies.Add(new Models.YoutubeReply
                    {
                        ReplyId = reply.Id,
                        ParentCommentId = thread.Id,
                        AuthorName = rSnippet?.AuthorDisplayName,
                        TextOriginal = rSnippet?.TextOriginal,
                        LikeCount = (long?)rSnippet?.LikeCount,
                        PublishedDate = rSnippet?.PublishedAtDateTimeOffset?.UtcDateTime,
                        UpdatedDate = rSnippet?.UpdatedAtDateTimeOffset?.UtcDateTime
                    });
                }
            }
        }

        if (dbComments.Any()) await _repository.SaveCommentsAsync(dbComments);
        if (dbReplies.Any()) await _repository.SaveRepliesAsync(dbReplies);

        // Captions
        var captions = await _apiService.GetCaptionsAsync(videoId, cancellationToken);
        var dbCaptions = captions.Select(c => new Models.YoutubeCaption
        {
            CaptionId = c.Id,
            VideoId = videoId,
            Language = c.Snippet?.Language,
            Name = c.Snippet?.Name,
            TrackKind = c.Snippet?.TrackKind,
            IsDraft = c.Snippet?.IsDraft,
            IsAutoSynced = c.Snippet?.IsAutoSynced
        }).ToList();

        if (dbCaptions.Any()) await _repository.SaveCaptionsAsync(dbCaptions);
    }
}
