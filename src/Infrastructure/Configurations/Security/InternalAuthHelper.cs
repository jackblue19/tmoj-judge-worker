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
    public static bool IsInternalRequest(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();

        if ( string.IsNullOrEmpty(ip) ) return false;

        return ip.StartsWith("10.104."); // private network của judge-server => chung 1 vpc
    }

    public static bool HasValidApiKey(HttpContext context , IConfiguration config)
    {
        var apiKey = context.Request.Headers["X-API-KEY"].FirstOrDefault();

        if ( string.IsNullOrEmpty(apiKey) ) return false;

        return apiKey == config["InternalAuth:JudgeApiKey"];
    }
}
