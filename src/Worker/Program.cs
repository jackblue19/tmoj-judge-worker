using Worker;

var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<Worker.Orchestration.SubmissionProcessor>();

builder.Services.AddScoped<Worker.Execution.Testset.TestsetEnsureService>();
builder.Services.AddScoped<Worker.Execution.Testset.TestsetPathResolver>();
builder.Services.AddScoped<Worker.Execution.Testset.TestsetLayoutAdapter>();

builder.Services.AddScoped<Worker.Execution.Containers.DockerSandboxRunner>();

builder.Services.AddScoped<Worker.Execution.Runtimes.IRuntimeExecutor , Worker.Execution.Runtimes.CompetitiveProgrammingExecutor>();

builder.Services.AddScoped<Worker.Orchestration.SubmissionProcessor>();

var host = builder.Build();
host.Run();
