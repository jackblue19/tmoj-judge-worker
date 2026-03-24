namespace WebAPI.Extensions;

public static class CorsExtensions
{
    public const string DefaultPolicyName = "DefaultCors";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services , IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName , policy =>
            {
                policy.WithOrigins(origins)
                      .WithMethods("GET" , "POST" , "PUT" , "PATCH" , "DELETE" , "OPTIONS")
                      .WithHeaders("Content-Type" , "Authorization" , "X-Correlation-Id")
                      .WithExposedHeaders("X-Correlation-Id")
                      .SetPreflightMaxAge(TimeSpan.FromHours(12));

                // Nếu code auth có dùng cookie auth (credentials) thì bật dòng dưới
                 policy.AllowCredentials();
            });
        });

        return services;
    }
}
