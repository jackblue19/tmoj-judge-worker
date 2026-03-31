using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configurations.Security;

public static class InternalAuthHelper
{
    public static bool HasValidApiKey(HttpContext httpContext , IConfiguration configuration)
    {
        var provided = httpContext.Request.Headers["X-API-KEY"].FirstOrDefault();
        if ( string.IsNullOrWhiteSpace(provided) )
            return false;

        var expected = configuration["InternalAuth:JudgeApiKey"];
        if ( string.IsNullOrWhiteSpace(expected) )
            return false;

        return string.Equals(provided , expected , StringComparison.Ordinal);
    }

    public static bool IsInternalRequest(HttpContext httpContext)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if ( string.IsNullOrWhiteSpace(remoteIp) )
            return false;

        return remoteIp.StartsWith("10.104.")
            || remoteIp.StartsWith("10.15.")
            || remoteIp == "127.0.0.1"
            || remoteIp == "::1";
    }
}