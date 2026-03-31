using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Threading;
using Worker.Orchestration;
using Worker.Serialization;

namespace Worker.Consumers;

public sealed class SubmissionCreatedConsumer : BackgroundService
{
    private readonly ILogger<SubmissionCreatedConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly Guid _workerId;

    public SubmissionCreatedConsumer(
        ILogger<SubmissionCreatedConsumer> logger ,
        IServiceScopeFactory scopeFactory ,
        IHttpClientFactory httpClientFactory ,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;

        _baseUrl = configuration["Judge:CallbackBaseUrl"]
            ?? throw new InvalidOperationException("Judge:CallbackBaseUrl is missing.");

        _apiKey = configuration["Judge:ApiKey"]
            ?? throw new InvalidOperationException("Judge:ApiKey is missing.");

        _workerId = Guid.Parse(configuration["Judge:WorkerId"]
            ?? throw new InvalidOperationException("Judge:WorkerId is missing."));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubmissionCreatedConsumer started. WorkerId={WorkerId}" , _workerId);

        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                var job = await TryClaimNextJobAsync(stoppingToken);

                if ( job is null )
                {
                    await Task.Delay(TimeSpan.FromSeconds(2) , stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<SubmissionProcessor>();

                await processor.ProcessAsync(job , stoppingToken);
            }
            catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
            {
                break;
            }
            catch ( Exception ex )
            {
                _logger.LogError(ex , "SubmissionCreatedConsumer loop crashed.");
                await Task.Delay(TimeSpan.FromSeconds(3) , stoppingToken);
            }
        }

        _logger.LogInformation("SubmissionCreatedConsumer stopped.");
    }

    private async Task<DispatchJudgeJobContract?> TryClaimNextJobAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(
            HttpMethod.Post ,
            $"{_baseUrl.TrimEnd('/')}/api/internal/judge/jobs/claim-next?workerId={_workerId}");

        req.Headers.Add("X-API-KEY" , _apiKey);

        using var res = await client.SendAsync(req , ct);

        if ( res.StatusCode == System.Net.HttpStatusCode.NoContent )
            return null;

        //res.EnsureSuccessStatusCode();
        if ( !res.IsSuccessStatusCode )
        {
            _logger.LogWarning("Claim failed: {Status}" , res.StatusCode);
            return null;
        }

        var job = await res.Content.ReadFromJsonAsync(
                                WorkerJsonSerializerContext.Default.DispatchJudgeJobContract ,
                                cancellationToken: ct);

        return job;
    }
}