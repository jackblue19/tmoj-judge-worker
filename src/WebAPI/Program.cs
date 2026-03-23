using Application;
using Application.UseCases.Problems.Queries.GetAllProblems;
using Domain.Abstractions;
using Infrastructure;
using Infrastructure.Configurations.Auth;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using System.Net.WebSockets;
using System.Text;
using WebAPI.Extensions;
using WebAPI.Judging;
using WebAPI.Middlewares;
using WebAPI.OData;
using Microsoft.AspNetCore.HttpOverrides;
using Application.UseCases.Auth;
using Infrastructure.ExternalServices.Identity;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//  database
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
builder.Services.AddControllers().AddOData(opt =>
{
    opt.AddRouteComponents("odata" , EdmModelBuilder.GetEdmModel())
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100);
});

builder.Services.AddScoped(
    typeof(IReadRepository<,>) ,
    typeof(EfReadRepository<,>));
builder.Services.AddScoped(
    typeof(IWriteRepository<,>) ,
    typeof(EfWriteRepository<,>));
builder.Services.AddScoped<IUnitOfWork , EfUnitOfWork>();

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

//  DI LOCAL JUDGE

builder.Services.AddSingleton<JudgeConnectionRegistry>();
builder.Services.AddSingleton<JudgeDispatchService>();
builder.Services.AddScoped<LocalJudgeService>();
builder.Services.AddHostedService<JudgeBridgeBackgroundService>();
builder.Services.AddControllers();

//builder.WebHost.UseUrls("http://+:8080"); //  comment cái này là test local được deploy thì mở ra


//  mediatr -> sau này refactor thì sẽ dùng
//builder.Services.AddMediatR(cfg =>
//    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
//builder.Services.AddMediatR(cfg =>
//{
//    cfg.RegisterServicesFromAssembly(
//        typeof(GetProblemsQuery).Assembly);
//});
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
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
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



//  minimal apis    +   judge-server (vnoj-tier)
app.MapGet("/health" , () => Results.Ok(new
{
    status = "Healthy" ,
    timestamp = DateTime.UtcNow ,
    author = "Jack Blue" ,
    apiVersion = "ver_2"
}));


app.Run();