using Application.Common.Pagination;
using Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebAPI.Models.Common;

namespace WebAPI.Extensions;

public sealed class AutoWrapResponseFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context , ResultExecutionDelegate next)
    {
        // Skip toàn bộ internal APIs: machine-to-machine contracts must stay raw
        if ( IsInternalApi(context) )
            return next();

        // Skip các response đặc biệt
        if ( context.Result is FileResult
            || context.Result is EmptyResult
            || context.Result is NoContentResult
            || context.Result is RedirectResult
            || context.Result is RedirectToActionResult
            || context.Result is RedirectToRouteResult )
        {
            return next();
        }

        // Chỉ wrap ObjectResult (OkObjectResult, CreatedAt..., etc.)
        if ( context.Result is ObjectResult obj )
        {
            // Không wrap lỗi ProblemDetails
            if ( obj.Value is ProblemDetails )
                return next();

            // Không wrap nếu đã là ApiResponse/ApiPagedResponse
            if ( IsAlreadyWrapped(obj.Value) )
                return next();

            var traceId = context.HttpContext.TraceIdentifier;

            // Nếu là PagedResult<T> => ApiPagedResponse<T>
            if ( TryWrapPagedResult(obj , traceId , out var wrappedPaged) )
            {
                context.Result = wrappedPaged;
                return next();
            }

            // Bình thường => ApiResponse<T>
            context.Result = WrapNormal(obj , traceId);
        }

        return next();
    }

    private static bool IsInternalApi(ResultExecutingContext context)
    {
        var path = context.HttpContext.Request.Path;
        return path.StartsWithSegments("/api/internal" , StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAlreadyWrapped(object? value)
    {
        if ( value is null ) return false;

        var t = value.GetType();
        if ( !t.IsGenericType ) return false;

        var def = t.GetGenericTypeDefinition();
        return def == typeof(ApiResponse<>) || def == typeof(ApiPagedResponse<>);
    }

    private static IActionResult WrapNormal(ObjectResult obj , string traceId)
    {
        var status = obj.StatusCode;
        var value = obj.Value ?? new object();
        var valueType = obj.Value?.GetType() ?? typeof(object);

        var apiType = typeof(ApiResponse<>).MakeGenericType(valueType);
        var apiValue = Activator.CreateInstance(apiType , value , null , traceId);

        return new ObjectResult(apiValue) { StatusCode = status };
    }

    private static bool TryWrapPagedResult(ObjectResult obj , string traceId , out IActionResult wrapped)
    {
        wrapped = default!;

        if ( obj.Value is null ) return false;

        var t = obj.Value.GetType();
        if ( !t.IsGenericType ) return false;
        if ( t.GetGenericTypeDefinition() != typeof(PagedResult<>) ) return false;

        var itemType = t.GetGenericArguments()[0];
        var itemsObj = t.GetProperty("Items")!.GetValue(obj.Value)!;

        var page = (int) t.GetProperty("Page")!.GetValue(obj.Value)!;
        var pageSize = (int) t.GetProperty("PageSize")!.GetValue(obj.Value)!;
        var totalCount = (long) t.GetProperty("TotalCount")!.GetValue(obj.Value)!;
        var totalPages = (long) t.GetProperty("TotalPages")!.GetValue(obj.Value)!;
        var hasPrev = (bool) t.GetProperty("HasPrevious")!.GetValue(obj.Value)!;
        var hasNext = (bool) t.GetProperty("HasNext")!.GetValue(obj.Value)!;

        var meta = new PaginationMeta(page , pageSize , totalCount , totalPages , hasPrev , hasNext);

        var apiPagedType = typeof(ApiPagedResponse<>).MakeGenericType(itemType);
        var apiPagedValue = Activator.CreateInstance(apiPagedType , itemsObj , meta , null , traceId);

        wrapped = new ObjectResult(apiPagedValue) { StatusCode = obj.StatusCode };
        return true;
    }
}