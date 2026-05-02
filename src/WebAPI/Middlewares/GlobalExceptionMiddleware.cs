using System.Text.Json;
using Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next , ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch ( OperationCanceledException )
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch ( ArgumentException ex )
        {
            await WriteProblemDetails(context , StatusCodes.Status400BadRequest , "Bad request" , ex.Message);
        }
        catch ( KeyNotFoundException ex )
        {
            await WriteProblemDetails(context , StatusCodes.Status404NotFound , "Not found" , ex.Message);
        }
        catch ( UnauthorizedAccessException ex )
        {
            await WriteProblemDetails(context , StatusCodes.Status401Unauthorized , "Unauthorized" , ex.Message);
        }
        catch ( InvalidOperationException ex )
        {
            await WriteProblemDetails(context , StatusCodes.Status409Conflict , "Conflict" , ex.Message);
        }
        catch ( NotFoundException ex )
        {
            await WriteProblemDetails(context , StatusCodes.Status404NotFound , "Not found" , ex.Message);
        }
        catch ( ConflictException ex )
        {
            await WriteProblemDetails(context , StatusCodes.Status409Conflict , "Conflict" , ex.Message);
        }
        catch ( Exception ex )
        {
            _logger.LogError(ex , "Unhandled exception");
            await WriteProblemDetails(context ,
                StatusCodes.Status500InternalServerError ,
                "Internal server error" ,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context ,
        int statusCode ,
        string title ,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = new ProblemDetails
        {
            Status = statusCode ,
            Title = title ,
            Detail = detail ,
            Instance = context.Request.Path
        };

        payload.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }
}