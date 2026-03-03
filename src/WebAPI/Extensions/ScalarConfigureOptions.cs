using Asp.Versioning;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebAPI.Filters;

namespace WebAPI.Extensions;

public static class ScalarConfigureOptions
{
    public static IServiceCollection AddScalarWithApiVersioning(this IServiceCollection services , IConfiguration configuration)
    {
        // API Versioning (URL segment: /api/v{version}/...)
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1 , 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;

                options.ApiVersionReader = ApiVersionReader.Combine(
                                    new UrlSegmentApiVersionReader());
            })
            // IMPORTANT: AddApiExplorer phải chain từ AddApiVersioning (IApiVersioningBuilder)
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";         // v1, v2...
                options.SubstituteApiVersionInUrl = true; // thay {version:apiVersion} bằng số version
            });

        // Swagger/OpenAPI generation (Swashbuckle) — đúng hướng “Swashbuckle” trong doc Scalar
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.OperationFilter<FileUploadOperationFilter>();
        });

        // Tự tạo SwaggerDoc cho từng version (v1, v2, ...) từ ApiExplorer
        services.AddTransient<IConfigureOptions<SwaggerGenOptions> , ConfigureSwaggerOptions>();

        return services;
    }

    public static WebApplication UseScalarUI(this WebApplication app)
    {
        app.MapSwagger("/openapi/{documentName}.json");  // :contentReference[oaicite:2]{index=2}

        // Scalar UI at /scalar (default)
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("FPTU TMOJ API docs");

            // Thường không cần set route pattern vì Scalar mặc định đi theo /openapi/{documentName}.json
            // nhưng set rõ ràng để khỏi “mơ hồ”:
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json"); // :contentReference[oaicite:3]{index=3}

            options.WithTheme(ScalarTheme.BluePlanet);
        });

        //app.UseSwaggerUI();   //  đó nếu ae thích xài swagger thì sẽ mở cmt ra

        return app;
    }
}
