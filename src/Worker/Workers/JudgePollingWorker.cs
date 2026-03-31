using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Orchestration;
using Worker.Services;

namespace Worker.Workers;

public sealed class JudgePollingWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(3);

    private readonly ILogger<JudgePollingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Guid _workerId;

    public JudgePollingWorker(
        ILogger<JudgePollingWorker> logger ,
        IServiceProvider serviceProvider ,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var workerIdRaw = configuration["Judge:WorkerId"];
        if ( !Guid.TryParse(workerIdRaw , out var workerId) )
            throw new InvalidOperationException("Judge:WorkerId is missing or invalid.");

        _workerId = workerId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JudgePollingWorker started. WorkerId={WorkerId}" , _workerId);

        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var backend = scope.ServiceProvider.GetRequiredService<JudgeBackendClient>();
                var processor = scope.ServiceProvider.GetRequiredService<SubmissionProcessor>();

                var job = await backend.ClaimNextAsync(_workerId , stoppingToken);

                if ( job is null )
                {
                    await Task.Delay(IdleDelay , stoppingToken);
                    continue;
                }

                await processor.ProcessAsync(job , stoppingToken);
            }
            catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
            {
                break;
            }
            catch ( Exception ex )
            {
                _logger.LogError(ex , "JudgePollingWorker loop failed.");
                await Task.Delay(ErrorDelay , stoppingToken);
            }
        }

        _logger.LogInformation("JudgePollingWorker stopped. WorkerId={WorkerId}" , _workerId);
    }
}