IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeChannel')
BEGIN
    CREATE TABLE YoutubeChannel (
        ChannelId NVARCHAR(100) PRIMARY KEY,
        Title NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX),
        CustomUrl NVARCHAR(255),
        Country NVARCHAR(10),
        PublishedDate DATETIME2,
        UploadPlaylistId NVARCHAR(100),
        SubscriberCount BIGINT,
        VideoCount BIGINT,
        ViewCount BIGINT,
        HiddenSubscriberCount BIT,
        ThumbnailUrl NVARCHAR(1000),
        Keywords NVARCHAR(MAX),
        BrandingSettings NVARCHAR(MAX),
        LastSynced DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeVideo')
BEGIN
    CREATE TABLE YoutubeVideo (
        VideoId NVARCHAR(100) PRIMARY KEY,
        ChannelId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeChannel(ChannelId),
        PlaylistPosition INT,
        PublishTime DATETIME2,
        Title NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX),
        Tags NVARCHAR(MAX),
        CategoryId NVARCHAR(50),
        DefaultLanguage NVARCHAR(50),
        DefaultAudioLanguage NVARCHAR(50),
        LiveBroadcastContent NVARCHAR(50),
        Duration NVARCHAR(50),
        Dimension NVARCHAR(10),
        Definition NVARCHAR(10),
        Caption BIT,
        LicensedContent BIT,
        Projection NVARCHAR(50),
        ViewCount BIGINT,
        LikeCount BIGINT,
        FavoriteCount BIGINT,
        CommentCount BIGINT,
        PrivacyStatus NVARCHAR(50),
        UploadStatus NVARCHAR(50),
        License NVARCHAR(50),
        Embeddable BIT,
        PublicStatsViewable BIT,
        MadeForKids BIT,
        SelfDeclaredMadeForKids BIT,
        HasProductPlacement BIT,
        LastSynced DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_YoutubeVideo_ChannelId ON YoutubeVideo(ChannelId);
    CREATE INDEX IX_YoutubeVideo_PublishTime ON YoutubeVideo(PublishTime);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeComment')
BEGIN
    CREATE TABLE YoutubeComment (
        CommentId NVARCHAR(100) PRIMARY KEY,
        VideoId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeVideo(VideoId),
        AuthorName NVARCHAR(255),
        AuthorChannelId NVARCHAR(100),
        AuthorProfileUrl NVARCHAR(1000),
        AuthorAvatarUrl NVARCHAR(1000),
        TextDisplay NVARCHAR(MAX),
        TextOriginal NVARCHAR(MAX),
        LikeCount BIGINT,
        PublishedDate DATETIME2,
        UpdatedDate DATETIME2,
        CanRate BIT,
        ViewerRating NVARCHAR(50),
        ModerationStatus NVARCHAR(50),
        TotalReplyCount INT,
        LastSynced DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_YoutubeComment_VideoId ON YoutubeComment(VideoId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeReply')
BEGIN
    CREATE TABLE YoutubeReply (
        ReplyId NVARCHAR(100) PRIMARY KEY,
        ParentCommentId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeComment(CommentId),
        AuthorName NVARCHAR(255),
        TextOriginal NVARCHAR(MAX),
        LikeCount BIGINT,
        PublishedDate DATETIME2,
        UpdatedDate DATETIME2,
        LastSynced DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_YoutubeReply_ParentCommentId ON YoutubeReply(ParentCommentId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeCaption')
BEGIN
    CREATE TABLE YoutubeCaption (
        CaptionId NVARCHAR(100) PRIMARY KEY,
        VideoId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeVideo(VideoId),
        Language NVARCHAR(50),
        Name NVARCHAR(255),
        TrackKind NVARCHAR(50),
        IsDraft BIT,
        IsAutoSynced BIT,
        LastSynced DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_YoutubeCaption_VideoId ON YoutubeCaption(VideoId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeChannelLinks')
BEGIN
    CREATE TABLE YoutubeChannelLinks (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ChannelId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeChannel(ChannelId),
        OriginalUrl NVARCHAR(MAX),
        ResolvedUrl NVARCHAR(MAX),
        Domain NVARCHAR(255),
        RootDomain NVARCHAR(255),
        Platform NVARCHAR(100),
        Category NVARCHAR(100),
        Source NVARCHAR(100),
        FirstSeen DATETIME2,
        LastSeen DATETIME2,
        InsertedOn DATETIME2 DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YoutubeVideoLinks')
BEGIN
    CREATE TABLE YoutubeVideoLinks (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VideoId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeVideo(VideoId),
        ChannelId NVARCHAR(100) NOT NULL FOREIGN KEY REFERENCES YoutubeChannel(ChannelId),
        OriginalUrl NVARCHAR(MAX),
        ResolvedUrl NVARCHAR(MAX),
        Domain NVARCHAR(255),
        RootDomain NVARCHAR(255),
        Platform NVARCHAR(100),
        Category NVARCHAR(100),
        Source NVARCHAR(100),
        PublishedDate DATETIME2,
        InsertedOn DATETIME2 DEFAULT SYSUTCDATETIME(),
        UpdatedOn DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Stored Procedures

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeChannel
    @ChannelId NVARCHAR(100),
    @Title NVARCHAR(255),
    @Description NVARCHAR(MAX),
    @CustomUrl NVARCHAR(255),
    @Country NVARCHAR(10),
    @PublishedDate DATETIME2,
    @UploadPlaylistId NVARCHAR(100),
    @SubscriberCount BIGINT,
    @VideoCount BIGINT,
    @ViewCount BIGINT,
    @HiddenSubscriberCount BIT,
    @ThumbnailUrl NVARCHAR(1000),
    @Keywords NVARCHAR(MAX),
    @BrandingSettings NVARCHAR(MAX)
AS
BEGIN
    MERGE YoutubeChannel AS target
    USING (SELECT @ChannelId AS ChannelId) AS source
    ON target.ChannelId = source.ChannelId
    WHEN MATCHED THEN
        UPDATE SET
            Title = @Title,
            Description = @Description,
            CustomUrl = @CustomUrl,
            Country = @Country,
            PublishedDate = @PublishedDate,
            UploadPlaylistId = @UploadPlaylistId,
            SubscriberCount = @SubscriberCount,
            VideoCount = @VideoCount,
            ViewCount = @ViewCount,
            HiddenSubscriberCount = @HiddenSubscriberCount,
            ThumbnailUrl = @ThumbnailUrl,
            Keywords = @Keywords,
            BrandingSettings = @BrandingSettings,
            LastSynced = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (ChannelId, Title, Description, CustomUrl, Country, PublishedDate, UploadPlaylistId, SubscriberCount, VideoCount, ViewCount, HiddenSubscriberCount, ThumbnailUrl, Keywords, BrandingSettings)
        VALUES (@ChannelId, @Title, @Description, @CustomUrl, @Country, @PublishedDate, @UploadPlaylistId, @SubscriberCount, @VideoCount, @ViewCount, @HiddenSubscriberCount, @ThumbnailUrl, @Keywords, @BrandingSettings);
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeVideo
    @VideoId NVARCHAR(100),
    @ChannelId NVARCHAR(100),
    @PlaylistPosition INT,
    @PublishTime DATETIME2,
    @Title NVARCHAR(255),
    @Description NVARCHAR(MAX),
    @Tags NVARCHAR(MAX),
    @CategoryId NVARCHAR(50),
    @DefaultLanguage NVARCHAR(50),
    @DefaultAudioLanguage NVARCHAR(50),
    @LiveBroadcastContent NVARCHAR(50),
    @Duration NVARCHAR(50),
    @Dimension NVARCHAR(10),
    @Definition NVARCHAR(10),
    @Caption BIT,
    @LicensedContent BIT,
    @Projection NVARCHAR(50),
    @ViewCount BIGINT,
    @LikeCount BIGINT,
    @FavoriteCount BIGINT,
    @CommentCount BIGINT,
    @PrivacyStatus NVARCHAR(50),
    @UploadStatus NVARCHAR(50),
    @License NVARCHAR(50),
    @Embeddable BIT,
    @PublicStatsViewable BIT,
    @MadeForKids BIT,
    @SelfDeclaredMadeForKids BIT,
    @HasProductPlacement BIT
AS
BEGIN
    MERGE YoutubeVideo AS target
    USING (SELECT @VideoId AS VideoId) AS source
    ON target.VideoId = source.VideoId
    WHEN MATCHED THEN
        UPDATE SET
            PlaylistPosition = ISNULL(@PlaylistPosition, PlaylistPosition),
            PublishTime = ISNULL(@PublishTime, PublishTime),
            Title = ISNULL(@Title, Title),
            Description = ISNULL(@Description, Description),
            Tags = ISNULL(@Tags, Tags),
            CategoryId = ISNULL(@CategoryId, CategoryId),
            DefaultLanguage = ISNULL(@DefaultLanguage, DefaultLanguage),
            DefaultAudioLanguage = ISNULL(@DefaultAudioLanguage, DefaultAudioLanguage),
            LiveBroadcastContent = ISNULL(@LiveBroadcastContent, LiveBroadcastContent),
            Duration = ISNULL(@Duration, Duration),
            Dimension = ISNULL(@Dimension, Dimension),
            Definition = ISNULL(@Definition, Definition),
            Caption = ISNULL(@Caption, Caption),
            LicensedContent = ISNULL(@LicensedContent, LicensedContent),
            Projection = ISNULL(@Projection, Projection),
            ViewCount = @ViewCount,
            LikeCount = @LikeCount,
            FavoriteCount = @FavoriteCount,
            CommentCount = @CommentCount,
            PrivacyStatus = ISNULL(@PrivacyStatus, PrivacyStatus),
            UploadStatus = ISNULL(@UploadStatus, UploadStatus),
            License = ISNULL(@License, License),
            Embeddable = ISNULL(@Embeddable, Embeddable),
            PublicStatsViewable = ISNULL(@PublicStatsViewable, PublicStatsViewable),
            MadeForKids = ISNULL(@MadeForKids, MadeForKids),
            SelfDeclaredMadeForKids = ISNULL(@SelfDeclaredMadeForKids, SelfDeclaredMadeForKids),
            HasProductPlacement = ISNULL(@HasProductPlacement, HasProductPlacement),
            LastSynced = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (VideoId, ChannelId, PlaylistPosition, PublishTime, Title, Description, Tags, CategoryId, DefaultLanguage, DefaultAudioLanguage, LiveBroadcastContent, Duration, Dimension, Definition, Caption, LicensedContent, Projection, ViewCount, LikeCount, FavoriteCount, CommentCount, PrivacyStatus, UploadStatus, License, Embeddable, PublicStatsViewable, MadeForKids, SelfDeclaredMadeForKids, HasProductPlacement)
        VALUES (@VideoId, @ChannelId, @PlaylistPosition, @PublishTime, @Title, @Description, @Tags, @CategoryId, @DefaultLanguage, @DefaultAudioLanguage, @LiveBroadcastContent, @Duration, @Dimension, @Definition, @Caption, @LicensedContent, @Projection, @ViewCount, @LikeCount, @FavoriteCount, @CommentCount, @PrivacyStatus, @UploadStatus, @License, @Embeddable, @PublicStatsViewable, @MadeForKids, @SelfDeclaredMadeForKids, @HasProductPlacement);
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeComment
    @CommentId NVARCHAR(100),
    @VideoId NVARCHAR(100),
    @AuthorName NVARCHAR(255),
    @AuthorChannelId NVARCHAR(100),
    @AuthorProfileUrl NVARCHAR(1000),
    @AuthorAvatarUrl NVARCHAR(1000),
    @TextDisplay NVARCHAR(MAX),
    @TextOriginal NVARCHAR(MAX),
    @LikeCount BIGINT,
    @PublishedDate DATETIME2,
    @UpdatedDate DATETIME2,
    @CanRate BIT,
    @ViewerRating NVARCHAR(50),
    @ModerationStatus NVARCHAR(50),
    @TotalReplyCount INT
AS
BEGIN
    MERGE YoutubeComment AS target
    USING (SELECT @CommentId AS CommentId) AS source
    ON target.CommentId = source.CommentId
    WHEN MATCHED THEN
        UPDATE SET
            LikeCount = @LikeCount,
            UpdatedDate = @UpdatedDate,
            ViewerRating = @ViewerRating,
            ModerationStatus = @ModerationStatus,
            TotalReplyCount = @TotalReplyCount,
            LastSynced = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (CommentId, VideoId, AuthorName, AuthorChannelId, AuthorProfileUrl, AuthorAvatarUrl, TextDisplay, TextOriginal, LikeCount, PublishedDate, UpdatedDate, CanRate, ViewerRating, ModerationStatus, TotalReplyCount)
        VALUES (@CommentId, @VideoId, @AuthorName, @AuthorChannelId, @AuthorProfileUrl, @AuthorAvatarUrl, @TextDisplay, @TextOriginal, @LikeCount, @PublishedDate, @UpdatedDate, @CanRate, @ViewerRating, @ModerationStatus, @TotalReplyCount);
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeReply
    @ReplyId NVARCHAR(100),
    @ParentCommentId NVARCHAR(100),
    @AuthorName NVARCHAR(255),
    @TextOriginal NVARCHAR(MAX),
    @LikeCount BIGINT,
    @PublishedDate DATETIME2,
    @UpdatedDate DATETIME2
AS
BEGIN
    MERGE YoutubeReply AS target
    USING (SELECT @ReplyId AS ReplyId) AS source
    ON target.ReplyId = source.ReplyId
    WHEN MATCHED THEN
        UPDATE SET
            LikeCount = @LikeCount,
            UpdatedDate = @UpdatedDate,
            LastSynced = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (ReplyId, ParentCommentId, AuthorName, TextOriginal, LikeCount, PublishedDate, UpdatedDate)
        VALUES (@ReplyId, @ParentCommentId, @AuthorName, @TextOriginal, @LikeCount, @PublishedDate, @UpdatedDate);
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeCaption
    @CaptionId NVARCHAR(100),
    @VideoId NVARCHAR(100),
    @Language NVARCHAR(50),
    @Name NVARCHAR(255),
    @TrackKind NVARCHAR(50),
    @IsDraft BIT,
    @IsAutoSynced BIT
AS
BEGIN
    MERGE YoutubeCaption AS target
    USING (SELECT @CaptionId AS CaptionId) AS source
    ON target.CaptionId = source.CaptionId
    WHEN MATCHED THEN
        UPDATE SET
            Language = @Language,
            Name = @Name,
            TrackKind = @TrackKind,
            IsDraft = @IsDraft,
            IsAutoSynced = @IsAutoSynced,
            LastSynced = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (CaptionId, VideoId, Language, Name, TrackKind, IsDraft, IsAutoSynced)
        VALUES (@CaptionId, @VideoId, @Language, @Name, @TrackKind, @IsDraft, @IsAutoSynced);
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeChannelLink
    @ChannelId NVARCHAR(100),
    @OriginalUrl NVARCHAR(MAX),
    @ResolvedUrl NVARCHAR(MAX),
    @Domain NVARCHAR(255),
    @RootDomain NVARCHAR(255),
    @Platform NVARCHAR(100),
    @Category NVARCHAR(100),
    @Source NVARCHAR(100)
AS
BEGIN
    DECLARE @Hash NVARCHAR(64) = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', CAST(@ChannelId AS NVARCHAR(MAX)) + @OriginalUrl), 2);

    MERGE YoutubeChannelLinks AS target
    USING (SELECT @ChannelId AS ChannelId, @OriginalUrl AS OriginalUrl) AS source
    ON target.ChannelId = source.ChannelId AND target.OriginalUrl = source.OriginalUrl
    WHEN MATCHED THEN
        UPDATE SET
            ResolvedUrl = @ResolvedUrl,
            Domain = @Domain,
            RootDomain = @RootDomain,
            Platform = @Platform,
            Category = @Category,
            Source = @Source,
            LastSeen = SYSUTCDATETIME(),
            UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (ChannelId, OriginalUrl, ResolvedUrl, Domain, RootDomain, Platform, Category, Source, FirstSeen, LastSeen)
        VALUES (@ChannelId, @OriginalUrl, @ResolvedUrl, @Domain, @RootDomain, @Platform, @Category, @Source, SYSUTCDATETIME(), SYSUTCDATETIME());
END
GO

CREATE OR ALTER PROCEDURE sp_UpsertYoutubeVideoLink
    @VideoId NVARCHAR(100),
    @ChannelId NVARCHAR(100),
    @OriginalUrl NVARCHAR(MAX),
    @ResolvedUrl NVARCHAR(MAX),
    @Domain NVARCHAR(255),
    @RootDomain NVARCHAR(255),
    @Platform NVARCHAR(100),
    @Category NVARCHAR(100),
    @Source NVARCHAR(100),
    @PublishedDate DATETIME2
AS
BEGIN
    MERGE YoutubeVideoLinks AS target
    USING (SELECT @VideoId AS VideoId, @OriginalUrl AS OriginalUrl) AS source
    ON target.VideoId = source.VideoId AND target.OriginalUrl = source.OriginalUrl
    WHEN MATCHED THEN
        UPDATE SET
            ResolvedUrl = @ResolvedUrl,
            Domain = @Domain,
            RootDomain = @RootDomain,
            Platform = @Platform,
            Category = @Category,
            Source = @Source,
            UpdatedOn = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (VideoId, ChannelId, OriginalUrl, ResolvedUrl, Domain, RootDomain, Platform, Category, Source, PublishedDate)
        VALUES (@VideoId, @ChannelId, @OriginalUrl, @ResolvedUrl, @Domain, @RootDomain, @Platform, @Category, @Source, @PublishedDate);
END
GO
