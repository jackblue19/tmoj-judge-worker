using Worker;

var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<Worker.Services.JudgeCallbackClient>();

builder.Services.AddScoped<Worker.Execution.Testset.TestsetEnsureService>();
builder.Services.AddScoped<Worker.Execution.Testset.TestsetPathResolver>();
builder.Services.AddScoped<Worker.Execution.Testset.TestsetLayoutAdapter>();

builder.Services.AddScoped<Worker.Execution.Containers.DockerSandboxRunner>();

builder.Services.AddScoped<Worker.Execution.Runtimes.Cp.CppExecutorProfile>();
builder.Services.AddScoped<Worker.Execution.Runtimes.Cp.JavaExecutorProfile>();
builder.Services.AddScoped<Worker.Execution.Runtimes.Cp.PythonExecutorProfile>();
builder.Services.AddScoped<Worker.Execution.Runtimes.RuntimeProfileRegistry>();

builder.Services.AddScoped<Worker.Execution.Runtimes.IRuntimeExecutor , Worker.Execution.Runtimes.CompetitiveProgrammingExecutor>();

builder.Services.AddScoped<Worker.Execution.JudgeEngine>();
builder.Services.AddScoped<Worker.Orchestration.SubmissionProcessor>();

builder.Services.AddHostedService<Worker.Consumers.SubmissionCreatedConsumer>();
builder.Services.AddHostedService<Worker.Services.JudgeWorkerHeartbeatService>();


var host = builder.Build();
host.Run();
