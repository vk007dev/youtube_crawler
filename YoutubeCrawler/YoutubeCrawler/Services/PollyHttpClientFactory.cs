using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Polly;

namespace YoutubeCrawler.Services;

public class PollyHttpClientFactory : Google.Apis.Http.HttpClientFactory
{
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public PollyHttpClientFactory(IAsyncPolicy<HttpResponseMessage> retryPolicy)
    {
        _retryPolicy = retryPolicy;
    }

    protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args)
    {
        var handler = base.CreateHandler(args);
        return new PolicyHttpMessageHandler(handler, _retryPolicy);
    }
}

public class PolicyHttpMessageHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public PolicyHttpMessageHandler(HttpMessageHandler innerHandler, IAsyncPolicy<HttpResponseMessage> policy)
        : base(innerHandler)
    {
        _policy = policy;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
    }
}
