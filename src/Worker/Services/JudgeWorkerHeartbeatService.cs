using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Worker.Serialization;

namespace Worker.Services;

public sealed class JudgeWorkerHeartbeatService : BackgroundService
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

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

        var workerIdRaw = configuration["Judge:WorkerId"];
        if ( !Guid.TryParse(workerIdRaw , out var workerId) )
            throw new InvalidOperationException("Judge:WorkerId is missing or invalid.");

        _payload = new JudgeWorkerHeartbeatContract
        {
            WorkerId = workerId ,
            Name = configuration["Judge:WorkerName"] ?? "judge-server-tmoj" ,
            Version = "v2" ,
            Capabilities =
            [
                "cp",
                "cpp",
                "java",
                "python",
                "docker",
                "vnoj-tier3"
            ] ,
            Status = "online"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "JudgeWorkerHeartbeatService started. WorkerId={WorkerId}, WorkerName={WorkerName}" ,
            _payload.WorkerId ,
            _payload.Name);

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

                if ( res.IsSuccessStatusCode )
                {
                    _logger.LogDebug(
                        "Heartbeat success. WorkerId={WorkerId}, StatusCode={StatusCode}" ,
                        _payload.WorkerId ,
                        (int) res.StatusCode);
                }
                else
                {
                    var body = await res.Content.ReadAsStringAsync(stoppingToken);
                    _logger.LogWarning(
                        "Heartbeat failed. WorkerId={WorkerId}, StatusCode={StatusCode}, Body={Body}" ,
                        _payload.WorkerId ,
                        (int) res.StatusCode ,
                        body);
                }
            }
            catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
            {
                break;
            }
            catch ( Exception ex )
            {
                _logger.LogWarning(ex , "Judge worker heartbeat failed. WorkerId={WorkerId}" , _payload.WorkerId);
            }

            await Task.Delay(HeartbeatInterval , stoppingToken);
        }

        _logger.LogInformation("JudgeWorkerHeartbeatService stopped. WorkerId={WorkerId}" , _payload.WorkerId);
    }
}