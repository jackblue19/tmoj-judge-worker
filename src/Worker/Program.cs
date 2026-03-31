using Worker.Execution;
using Worker.Execution.Containers;
using Worker.Execution.Runtimes;
using Worker.Execution.Runtimes.Cp;
using Worker.Execution.Testset;
using Worker.Orchestration;
using Worker.Services;
using Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// typed client for backend internal APIs
builder.Services.AddHttpClient<JudgeBackendClient>(client =>
{
    var baseUrl = builder.Configuration["JudgeBackend:BaseUrl"]
        ?? throw new InvalidOperationException("JudgeBackend:BaseUrl is missing.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// default client factory for heartbeat service
builder.Services.AddHttpClient();

builder.Services.AddScoped<TestsetEnsureService>();
builder.Services.AddScoped<TestsetPathResolver>();
builder.Services.AddScoped<TestsetLayoutAdapter>();

builder.Services.AddScoped<DockerSandboxRunner>();

builder.Services.AddScoped<CppExecutorProfile>();
builder.Services.AddScoped<JavaExecutorProfile>();
builder.Services.AddScoped<PythonExecutorProfile>();
builder.Services.AddScoped<RuntimeProfileRegistry>();

builder.Services.AddScoped<IRuntimeExecutor , CompetitiveProgrammingExecutor>();

builder.Services.AddScoped<JudgeEngine>();
builder.Services.AddScoped<LocalJudgeService>();
builder.Services.AddScoped<SubmissionProcessor>();

builder.Services.AddHostedService<JudgePollingWorker>();
builder.Services.AddHostedService<JudgeWorkerHeartbeatService>();

var host = builder.Build();
host.Run();