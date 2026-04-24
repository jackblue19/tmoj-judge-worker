using Application;
using Application.Common.Interfaces;
using Application.Common.Services;
using Application.UseCases.Contests.Queries;
using Infrastructure;
using Infrastructure.Configurations.Auth;
using Infrastructure.ExternalServices;
using Infrastructure.Persistence.Common.Repositories;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using System.Net.WebSockets;
using System.Text;
using WebAPI.Extensions;
//using WebAPI.Judging;
using WebAPI.Middlewares;
using WebAPI.OData;
using WebAPI.Services.Judging;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//  database
builder.Services.AddScoped<IProblemEditorialRepository, ProblemEditorialRepository>();
builder.Services.AddScoped<IProblemDiscussionRepository, ProblemDiscussionRepository>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IGamificationRepository, GamificationRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<IClassSlotRepository, ClassSlotRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddPostgresConnection(builder.Configuration);
builder.Services.AddDbContext<TmojDbContext>((sp , opt) =>
{
    if ( builder.Environment.IsDevelopment() )
    {
        opt.EnableDetailedErrors();
        opt.EnableSensitiveDataLogging();
        opt.LogTo(Console.WriteLine , LogLevel.Information);
    }
});

//  controller + odata
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    })
    .AddOData(opt =>
{
    opt.AddRouteComponents("odata" , EdmModelBuilder.GetEdmModel())
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100);
});

builder.Services.AddPersistence();
builder.Services.AddExternalServices(builder.Configuration);

//  jwt sample settings (rcm nen dung)
builder.Services.AddTraditionalJwtAuth(builder.Configuration);
builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Authentication:Google"));
builder.Services.Configure<GithubOptions>(builder.Configuration.GetSection("Authentication:Github"));

//  wrap + problem details + rate limit
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AutoWrapResponseFilter>();
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScalarWithApiVersioning(builder.Configuration);

//  DI TEMP SERVICES

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService , CurrentUserService>();
builder.Services.AddScoped<IContestStatusService , ContestStatusService>();

//  v2  -   Judge Worker    -   DI
builder.Services.AddScoped<WebAPI.Services.Judging.JudgeJobDispatchService>();
builder.Services.AddScoped<WebAPI.Services.Judging.JudgeResultApplyService>();

builder.Services.AddScoped<JudgeWorkerHeartbeatService>();
builder.Services.AddScoped<JudgeMetricsService>();

builder.Services.AddScoped<SubmissionQueryService>();
builder.Services.AddScoped<SubmissionRejudgeService>();

builder.Services.AddSignalR();

builder.Services.AddScoped<SubmissionNoteService>();
builder.Services.AddScoped<SubmissionRealtimeNotifier>();

//builder.WebHost.UseUrls("http://+:8080"); //  comment cái này là test local được deploy thì mở ra

builder.Services.AddApplication();



builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
                    | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
                    | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode;
});

//builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll" ,
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
//builder.Services.AddRateLimiting();

builder.Services.Configure<LocalStorageOptions>(
    builder.Configuration.GetSection("LocalStorage"));

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("ProblemsList" , policy =>
    {
        policy
            .Expire(TimeSpan.FromSeconds(30))
            .Tag("problems");
    });
});

//builder.Services.AddTransient(typeof(IPipelineBehavior<,>) , typeof(ValidationBehavior<,>));
//builder.Services.AddTransient(typeof(IPipelineBehavior<,>) , typeof(LoggingBehavior<,>));

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 100 MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if ( app.Environment.IsDevelopment() )
//{
app.UseSwagger();
app.UseSwaggerUI();
app.UseScalarUI();
//}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

//app.UseHttpsRedirection();
//if ( app.Environment.IsDevelopment() )
//{
//    app.UseHttpsRedirection();
//}
app.UseExceptionHandler();
app.UseRouting();

app.UseCors("AllowAll");

//app.UseCors(CorsExtensions.DefaultPolicyName);
//app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

//app.UseWebSockets();


app.Map("/bridge" , async context =>
{
    if ( !context.WebSockets.IsWebSocketRequest )
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket expected");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    Console.WriteLine("JUDGE CONNECTED");

    var buffer = new byte[8192];
    while ( socket.State == WebSocketState.Open )
    {
        var result = await socket.ReceiveAsync(buffer , CancellationToken.None);
        if ( result.MessageType == WebSocketMessageType.Close )
        {
            Console.WriteLine("JUDGE DISCONNECTED");
            break;
        }

        var msg = Encoding.UTF8.GetString(buffer , 0 , result.Count);
        Console.WriteLine($"FROM JUDGE: {msg}");
    }
});

app.MapControllers();
app.UseStatusCodePages();
app.UseHttpLogging();
app.UseMiddleware<RequestLogScopeMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapHub<WebAPI.Hubs.SubmissionHub>("/hubs/submissions");


//  minimal apis    +   judge-server (vnoj-tier)
app.MapGet("/health" , () => Results.Ok(new
{
    status = "Healthy" ,
    timestamp = DateTime.UtcNow ,
    author = "Jack Blue" ,
    apiVersion = "ver_2"
}));


app.Run();