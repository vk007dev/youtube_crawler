using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeCrawler.Models;

namespace YoutubeCrawler.Repositories;

public class YoutubeRepository
{
    private readonly string _connectionString;
    private readonly ILogger<YoutubeRepository> _logger;

    public YoutubeRepository(IOptions<CrawlerConfig> config, ILogger<YoutubeRepository> logger)
    {
        _connectionString = config.Value.ConnectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<DateTime?> GetLatestVideoPublishTimeAsync(string channelId)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<DateTime?>(
            "SELECT MAX(PublishTime) FROM YoutubeVideo WHERE ChannelId = @ChannelId",
            new { ChannelId = channelId }
        );
    }

    public async Task<IEnumerable<string>> GetExistingVideoIdsAsync(string channelId)
    {
        using var db = CreateConnection();
        return await db.QueryAsync<string>(
            "SELECT VideoId FROM YoutubeVideo WHERE ChannelId = @ChannelId",
            new { ChannelId = channelId }
        );
    }

    public async Task SaveChannelAsync(YoutubeChannel channel)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync("sp_UpsertYoutubeChannel", channel, commandType: CommandType.StoredProcedure);
    }

    public async Task SaveVideosAsync(IEnumerable<YoutubeVideo> videos)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            foreach (var video in videos)
            {
                await db.ExecuteAsync("sp_UpsertYoutubeVideo", video, transaction: tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
        }
        catch (System.Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to save videos batch.");
            throw;
        }
    }

    public async Task SaveCommentsAsync(IEnumerable<YoutubeComment> comments)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            foreach (var comment in comments)
            {
                await db.ExecuteAsync("sp_UpsertYoutubeComment", comment, transaction: tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
        }
        catch (System.Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to save comments batch.");
            throw;
        }
    }

    public async Task SaveRepliesAsync(IEnumerable<YoutubeReply> replies)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            foreach (var reply in replies)
            {
                await db.ExecuteAsync("sp_UpsertYoutubeReply", reply, transaction: tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
        }
        catch (System.Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to save replies batch.");
            throw;
        }
    }

    public async Task SaveCaptionsAsync(IEnumerable<YoutubeCaption> captions)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            foreach (var caption in captions)
            {
                await db.ExecuteAsync("sp_UpsertYoutubeCaption", caption, transaction: tx, commandType: CommandType.StoredProcedure);
            }
            tx.Commit();
        }
        catch (System.Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to save captions batch.");
            throw;
        }
    }

    public async Task SaveLinksAsync(IEnumerable<YoutubeLink> links)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            foreach (var link in links)
            {
                if (link.VideoId != null)
                {
                    await db.ExecuteAsync("sp_UpsertYoutubeVideoLink", link, transaction: tx, commandType: CommandType.StoredProcedure);
                }
                else if (link.ChannelId != null)
                {
                    await db.ExecuteAsync("sp_UpsertYoutubeChannelLink", link, transaction: tx, commandType: CommandType.StoredProcedure);
                }
            }
            tx.Commit();
        }
        catch (System.Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Failed to save links batch.");
            throw;
        }
    }
}
