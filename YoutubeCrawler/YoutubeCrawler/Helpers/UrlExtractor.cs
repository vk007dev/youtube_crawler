using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YoutubeCrawler.Models;

namespace YoutubeCrawler.Helpers;

public class UrlExtractor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UrlExtractor> _logger;

    public UrlExtractor(HttpClient httpClient, ILogger<UrlExtractor> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<YoutubeLink>> ExtractUrlsAsync(string text, string source, string? channelId, string? videoId, DateTime? publishedDate)
    {
        var links = new List<YoutubeLink>();
        if (string.IsNullOrWhiteSpace(text)) return links;

        // Regex to extract URLs
        var regex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var matches = regex.Matches(text);

        var uniqueUrls = matches.Select(m => m.Value).Distinct().ToList();

        foreach (var url in uniqueUrls)
        {
            try
            {
                var resolvedUrl = await ResolveUrlAsync(url);
                var uri = new Uri(resolvedUrl);

                var link = new YoutubeLink
                {
                    ChannelId = channelId,
                    VideoId = videoId,
                    OriginalUrl = url,
                    ResolvedUrl = resolvedUrl,
                    Domain = uri.Host,
                    RootDomain = GetRootDomain(uri.Host),
                    Platform = GetPlatform(uri.Host),
                    Category = ClassifyUrl(uri.Host),
                    Source = source,
                    PublishedDate = publishedDate
                };
                links.Add(link);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process URL: {Url}", url);

                // Add with basic info if resolve fails
                try {
                     var uri = new Uri(url);
                     links.Add(new YoutubeLink
                     {
                        ChannelId = channelId,
                        VideoId = videoId,
                        OriginalUrl = url,
                        ResolvedUrl = url,
                        Domain = uri.Host,
                        RootDomain = GetRootDomain(uri.Host),
                        Platform = GetPlatform(uri.Host),
                        Category = ClassifyUrl(uri.Host),
                        Source = source,
                        PublishedDate = publishedDate
                     });
                }
                catch { }
            }
        }

        return links;
    }

    private async Task<string> ResolveUrlAsync(string url)
    {
        string[] shorteners = { "bit.ly", "tinyurl", "t.co", "goo.gl", "buff.ly", "rebrand.ly", "ow.ly", "cutt.ly", "lnkd.in" };
        var uri = new Uri(url);

        if (shorteners.Any(s => uri.Host.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    response.StatusCode == System.Net.HttpStatusCode.Redirect ||
                    response.StatusCode == System.Net.HttpStatusCode.RedirectMethod ||
                    response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect ||
                    response.StatusCode == System.Net.HttpStatusCode.Moved)
                {
                    return response.Headers.Location?.ToString() ?? url;
                }
                return response.RequestMessage?.RequestUri?.ToString() ?? url;
            }
            catch
            {
                return url; // Return original on error
            }
        }
        return url;
    }

    private string GetRootDomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length >= 2)
        {
            return $"{parts[parts.Length - 2]}.{parts[parts.Length - 1]}";
        }
        return host;
    }

    private string GetPlatform(string host)
    {
        host = host.ToLower();
        if (host.Contains("facebook.com")) return "Facebook";
        if (host.Contains("instagram.com")) return "Instagram";
        if (host.Contains("twitter.com") || host.Contains("x.com")) return "X";
        if (host.Contains("tiktok.com")) return "TikTok";
        if (host.Contains("linkedin.com")) return "LinkedIn";
        if (host.Contains("youtube.com") || host.Contains("youtu.be")) return "YouTube";
        if (host.Contains("pinterest.com")) return "Pinterest";
        if (host.Contains("reddit.com")) return "Reddit";
        if (host.Contains("discord.com") || host.Contains("discord.gg")) return "Discord";
        if (host.Contains("t.me") || host.Contains("telegram.org")) return "Telegram";
        if (host.Contains("whatsapp.com")) return "WhatsApp";
        if (host.Contains("snapchat.com")) return "Snapchat";
        if (host.Contains("twitch.tv")) return "Twitch";
        if (host.Contains("vimeo.com")) return "Vimeo";
        if (host.Contains("github.com")) return "GitHub";
        if (host.Contains("medium.com")) return "Medium";
        if (host.Contains("patreon.com")) return "Patreon";
        if (host.Contains("ko-fi.com")) return "Ko-fi";
        if (host.Contains("buymeacoffee.com")) return "BuyMeACoffee";
        if (host.Contains("linktr.ee")) return "Linktree";
        if (host.Contains("beacons.ai")) return "Beacons";
        if (host.Contains("campsite.bio")) return "Campsite";
        if (host.Contains("amazon.")) return "Amazon";
        if (host.Contains("apple.com/app-store")) return "Apple App Store";
        if (host.Contains("play.google.com")) return "Google Play";
        if (host.Contains("shopify.com")) return "Shopify";

        return "Website";
    }

    private string ClassifyUrl(string host)
    {
        var platform = GetPlatform(host);
        if (platform != "Website") return platform; // Often platform maps to category

        // Add specific affiliate/store logic here if needed
        return "Website";
    }
}
