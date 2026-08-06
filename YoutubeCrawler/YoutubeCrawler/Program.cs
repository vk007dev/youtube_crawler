using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using YoutubeCrawler.Helpers;
using YoutubeCrawler.Models;
using YoutubeCrawler.Repositories;
using YoutubeCrawler.Services;

namespace YoutubeCrawler;

class Program
{
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting application...");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;
                services.Configure<CrawlerConfig>(config.GetSection("CrawlerConfig"));

                var crawlerConfig = config.GetSection("CrawlerConfig").Get<CrawlerConfig>() ?? new CrawlerConfig();

                services.AddSingleton<YoutubeRepository>();
                services.AddSingleton(provider =>
                {
                    return new YouTubeApiService(
                        provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CrawlerConfig>>(),
                        provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<YouTubeApiService>>(),
                        GetRetryPolicy(crawlerConfig.MaxRetryCount)
                    );
                });
                services.AddHttpClient<UrlExtractor>()
                        .AddPolicyHandler(GetRetryPolicy(crawlerConfig.MaxRetryCount));

                services.AddSingleton<UrlExtractor>();
                services.AddHostedService<CrawlerService>();
            });

    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles 5xx and 408
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // Handle 429
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
