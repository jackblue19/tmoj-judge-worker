using Application;
using Application.Common.Interfaces;
using Application.UseCases.Problems.Queries.GetAllProblems;
using Domain.Abstractions;
using Infrastructure;
using Infrastructure.Configurations.Auth;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Common.Repositories;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using WebAPI.Extensions;
using WebAPI.Judging;
using WebAPI.Middlewares;
using WebAPI.OData;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//  database
builder.Services.AddScoped<IEditorialRepository, EditorialRepository>();
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

//  DI
builder.Services.AddSingleton<LocalJudgeService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if ( app.Environment.IsDevelopment() )
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseScalarUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseRouting();

app.UseCors("AllowAll");

//app.UseCors(CorsExtensions.DefaultPolicyName);
//app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();
app.UseStatusCodePages();
app.UseHttpLogging();
app.UseMiddleware<RequestLogScopeMiddleware>();

//  minimal apis
app.MapGet("/health" , () => Results.Ok(new
{
    status = "Healthy" ,
    timestamp = DateTime.UtcNow
}));

app.Run();