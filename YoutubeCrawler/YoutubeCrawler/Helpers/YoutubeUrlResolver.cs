using System;
using System.Text.RegularExpressions;

namespace YoutubeCrawler.Helpers;

public class YoutubeUrlResolver
{
    public static (string Type, string Value) ParseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be empty", nameof(url));

        url = url.TrimEnd('/');

        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].TrimEnd('/');
                if (segment.Equals("channel", StringComparison.OrdinalIgnoreCase) && i + 1 < segments.Length)
                {
                    return ("channel", segments[i + 1].TrimEnd('/'));
                }
                if (segment.Equals("user", StringComparison.OrdinalIgnoreCase) && i + 1 < segments.Length)
                {
                    return ("user", segments[i + 1].TrimEnd('/'));
                }
                if (segment.Equals("c", StringComparison.OrdinalIgnoreCase) && i + 1 < segments.Length)
                {
                    return ("c", segments[i + 1].TrimEnd('/'));
                }
            }
        }
        catch { }

        var handleMatch = Regex.Match(url, @"@([a-zA-Z0-9_-]+)");
        if (handleMatch.Success)
        {
            return ("handle", handleMatch.Groups[1].Value);
        }

        throw new ArgumentException($"Cannot parse YouTube URL: {url}");
    }
}
