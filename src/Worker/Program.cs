using Worker;

var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<Worker.Orchestration.SubmissionProcessor>();

var host = builder.Build();
host.Run();
