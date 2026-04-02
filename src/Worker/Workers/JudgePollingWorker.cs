using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Contracts.Submissions.Judging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Orchestration;
using Worker.Services;

namespace Worker.Workers;

public sealed class JudgePollingWorker : BackgroundService
{
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    private readonly ILogger<JudgePollingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly Guid? _configuredWorkerId;
    private readonly string _workerName;
    private readonly string _workerVersion;
    private readonly int _maxParallelJobs;
    private readonly int _pollingIntervalMs;

    private readonly SemaphoreSlim _parallelGate;
    private readonly ConcurrentDictionary<Guid , byte> _runningJobs = new();

    private Guid _workerId;
    private DateTime _startedAtUtc;

    public JudgePollingWorker(
        ILogger<JudgePollingWorker> logger ,
        IServiceProvider serviceProvider ,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var workerIdRaw = configuration["Judge:WorkerId"];
        _configuredWorkerId = Guid.TryParse(workerIdRaw , out var wid) ? wid : null;

        _workerName = configuration["Judge:WorkerName"] ?? Environment.MachineName;
        _workerVersion = configuration["Judge:WorkerVersion"] ?? "1.0.0";

        var maxParallelRaw = configuration["Judge:MaxParallelJobs"];
        _maxParallelJobs = int.TryParse(maxParallelRaw , out var mpj) && mpj > 0 ? mpj : 1;

        var pollingRaw = configuration["Judge:PollingIntervalMs"];
        _pollingIntervalMs = int.TryParse(pollingRaw , out var poll) && poll > 0 ? poll : 1000;

        _parallelGate = new SemaphoreSlim(_maxParallelJobs , _maxParallelJobs);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _startedAtUtc = DateTime.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var backend = scope.ServiceProvider.GetRequiredService<JudgeBackendClient>();

        var registerReq = new JudgeWorkerRegistrationContract
        {
            WorkerId = _configuredWorkerId ,
            Name = _workerName ,
            Status = "starting" ,
            Version = _workerVersion ,
            MaxParallelJobs = _maxParallelJobs ,
            SupportedRuntimeProfileKeys = GetCapabilities()
        };

        _workerId = await backend.RegisterWorkerAsync(registerReq , cancellationToken);

        _logger.LogInformation(
            "JudgePollingWorker registered. WorkerId={WorkerId}, Name={WorkerName}, MaxParallelJobs={MaxParallelJobs}" ,
            _workerId , _workerName , _maxParallelJobs);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JudgePollingWorker started. WorkerId={WorkerId}" , _workerId);

        var heartbeatTask = RunHeartbeatLoopAsync(stoppingToken);

        try
        {
            while ( !stoppingToken.IsCancellationRequested )
            {
                try
                {
                    while ( _parallelGate.CurrentCount > 0 && !stoppingToken.IsCancellationRequested )
                    {
                        using var claimScope = _serviceProvider.CreateScope();

                        var backend = claimScope.ServiceProvider.GetRequiredService<JudgeBackendClient>();
                        var job = await backend.ClaimNextAsync(_workerId , stoppingToken);

                        if ( job is null )
                            break;

                        await _parallelGate.WaitAsync(stoppingToken);

                        _ = Task.Run(
                            async () =>
                            {
                                try
                                {
                                    _runningJobs.TryAdd(job.JobId , 0);

                                    using var processScope = _serviceProvider.CreateScope();
                                    var processor = processScope.ServiceProvider.GetRequiredService<SubmissionProcessor>();

                                    await processor.ProcessAsync(job , stoppingToken);
                                }
                                catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
                                {
                                }
                                catch ( Exception ex )
                                {
                                    _logger.LogError(ex ,
                                        "Judge job processing failed. JobId={JobId}, SubmissionId={SubmissionId}" ,
                                        job.JobId , job.SubmissionId);
                                }
                                finally
                                {
                                    _runningJobs.TryRemove(job.JobId , out _);
                                    _parallelGate.Release();
                                }
                            } ,
                            CancellationToken.None);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(_pollingIntervalMs) , stoppingToken);
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
        }
        finally
        {
            try
            {
                await heartbeatTask;
            }
            catch
            {
            }
        }

        _logger.LogInformation("JudgePollingWorker stopped. WorkerId={WorkerId}" , _workerId);
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken stoppingToken)
    {
        while ( !stoppingToken.IsCancellationRequested )
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var backend = scope.ServiceProvider.GetRequiredService<JudgeBackendClient>();

                await backend.HeartbeatAsync(BuildHeartbeat() , stoppingToken);
            }
            catch ( OperationCanceledException ) when ( stoppingToken.IsCancellationRequested )
            {
                break;
            }
            catch ( Exception ex )
            {
                _logger.LogWarning(ex , "Judge worker heartbeat failed. WorkerId={WorkerId}" , _workerId);
            }

            await Task.Delay(HeartbeatInterval , stoppingToken);
        }
    }

    private JudgeWorkerHeartbeatContract BuildHeartbeat()
    {
        var memInfo = GC.GetGCMemoryInfo();
        var totalAvailableBytes = memInfo.TotalAvailableMemoryBytes > 0
            ? memInfo.TotalAvailableMemoryBytes
            : 0;

        var usedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var uptime = DateTime.UtcNow - _startedAtUtc;

        return new JudgeWorkerHeartbeatContract
        {
            WorkerId = _workerId ,
            Name = _workerName ,
            Version = _workerVersion ,
            Capabilities = GetCapabilities() ,
            Status = _runningJobs.IsEmpty ? "online" : "busy" ,
            RunningJobs = _runningJobs.Count ,
            MaxParallelJobs = _maxParallelJobs ,
            CpuUsagePercent = null ,
            MemoryUsedMb = usedBytes / (1024 * 1024) ,
            MemoryTotalMb = totalAvailableBytes > 0 ? totalAvailableBytes / (1024 * 1024) : null ,
            LoadAverage1m = TryGetLoadAverage1m() ,
            UptimeSeconds = (long) uptime.TotalSeconds
        };
    }

    private static List<string> GetCapabilities()
    {
        return new List<string>
        {
            "cpp17-gcc",
            "java-default",
            "python3-default"
        };
    }

    private static double? TryGetLoadAverage1m()
    {
        try
        {
            if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/proc/loadavg") )
            {
                var raw = File.ReadAllText("/proc/loadavg").Trim();
                var first = raw.Split(' ' , StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if ( double.TryParse(
                    first ,
                    System.Globalization.NumberStyles.Float ,
                    System.Globalization.CultureInfo.InvariantCulture ,
                    out var value) )
                {
                    return value;
                }
            }
        }
        catch
        {
        }

        return null;
    }
}