using System;

namespace YoutubeCrawler.Models;

public class YoutubeChannel
{
    public string ChannelId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CustomUrl { get; set; }
    public string? Country { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? UploadPlaylistId { get; set; }
    public long? SubscriberCount { get; set; }
    public long? VideoCount { get; set; }
    public long? ViewCount { get; set; }
    public bool? HiddenSubscriberCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Keywords { get; set; }
    public string? BrandingSettings { get; set; }
}

public class YoutubeVideo
{
    public string VideoId { get; set; } = null!;
    public string ChannelId { get; set; } = null!;
    public int? PlaylistPosition { get; set; }
    public DateTime? PublishTime { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public string? CategoryId { get; set; }
    public string? DefaultLanguage { get; set; }
    public string? DefaultAudioLanguage { get; set; }
    public string? LiveBroadcastContent { get; set; }
    public string? Duration { get; set; }
    public string? Dimension { get; set; }
    public string? Definition { get; set; }
    public bool? Caption { get; set; }
    public bool? LicensedContent { get; set; }
    public string? Projection { get; set; }
    public long? ViewCount { get; set; }
    public long? LikeCount { get; set; }
    public long? FavoriteCount { get; set; }
    public long? CommentCount { get; set; }
    public string? PrivacyStatus { get; set; }
    public string? UploadStatus { get; set; }
    public string? License { get; set; }
    public bool? Embeddable { get; set; }
    public bool? PublicStatsViewable { get; set; }
    public bool? MadeForKids { get; set; }
    public bool? SelfDeclaredMadeForKids { get; set; }
    public bool? HasProductPlacement { get; set; }
}

public class YoutubeComment
{
    public string CommentId { get; set; } = null!;
    public string VideoId { get; set; } = null!;
    public string? AuthorName { get; set; }
    public string? AuthorChannelId { get; set; }
    public string? AuthorProfileUrl { get; set; }
    public string? AuthorAvatarUrl { get; set; }
    public string? TextDisplay { get; set; }
    public string? TextOriginal { get; set; }
    public long? LikeCount { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool? CanRate { get; set; }
    public string? ViewerRating { get; set; }
    public string? ModerationStatus { get; set; }
    public int? TotalReplyCount { get; set; }
}

public class YoutubeReply
{
    public string ReplyId { get; set; } = null!;
    public string ParentCommentId { get; set; } = null!;
    public string? AuthorName { get; set; }
    public string? TextOriginal { get; set; }
    public long? LikeCount { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class YoutubeCaption
{
    public string CaptionId { get; set; } = null!;
    public string VideoId { get; set; } = null!;
    public string? Language { get; set; }
    public string? Name { get; set; }
    public string? TrackKind { get; set; }
    public bool? IsDraft { get; set; }
    public bool? IsAutoSynced { get; set; }
}

public class YoutubeLink
{
    public string? ChannelId { get; set; }
    public string? VideoId { get; set; }
    public string OriginalUrl { get; set; } = null!;
    public string? ResolvedUrl { get; set; }
    public string? Domain { get; set; }
    public string? RootDomain { get; set; }
    public string? Platform { get; set; }
    public string? Category { get; set; }
    public string Source { get; set; } = null!;
    public DateTime? PublishedDate { get; set; }
}
