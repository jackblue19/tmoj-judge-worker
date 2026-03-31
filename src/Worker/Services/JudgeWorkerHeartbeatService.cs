using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Worker.Serialization;

namespace Worker.Services;

public sealed class JudgeWorkerHeartbeatService : BackgroundService
{
    private readonly ILogger<JudgeWorkerHeartbeatService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly JudgeWorkerHeartbeatContract _payload;

    public JudgeWorkerHeartbeatService(
        ILogger<JudgeWorkerHeartbeatService> logger ,
        IHttpClientFactory httpClientFactory ,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _baseUrl = configuration["Judge:CallbackBaseUrl"]
            ?? throw new InvalidOperationException("Judge:CallbackBaseUrl is missing.");

        _apiKey = configuration["Judge:ApiKey"]
            ?? throw new InvalidOperationException("Judge:ApiKey is missing.");

        _payload = new JudgeWorkerHeartbeatContract
        {
            WorkerId = Guid.Parse(configuration["Judge:WorkerId"]
                ?? throw new InvalidOperationException("Judge:WorkerId is missing.")) ,
            Name = configuration["Judge:WorkerName"] ?? "judge-server-tmoj" ,
            Version = "v2" ,
            Capabilities = new List<string>
                {
                    "cp",
                    "cpp",
                    "java",
                    "python",
                    "docker",
                    "vnoj-tier3"
                } ,
            Status = "online"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                using var req = new HttpRequestMessage(
                    HttpMethod.Post ,
                    $"{_baseUrl.TrimEnd('/')}/api/internal/judge/workers/heartbeat");

                req.Headers.Add("X-API-KEY" , _apiKey);

                req.Content = JsonContent.Create(
                    _payload ,
                    WorkerJsonSerializerContext.Default.JudgeWorkerHeartbeatContract);

                using var res = await client.SendAsync(req , stoppingToken);
                res.EnsureSuccessStatusCode();
            }
            catch ( Exception ex )
            {
                _logger.LogWarning(ex , "Judge worker heartbeat failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30) , stoppingToken);
        }
    }
}