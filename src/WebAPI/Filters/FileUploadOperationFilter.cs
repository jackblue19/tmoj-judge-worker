using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Filters;

public sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation , OperationFilterContext context)
    {
        var hasFormFile = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Type == typeof(IFormFile));

        if ( !hasFormFile ) return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties =
                        {
                            ["file"] = new OpenApiSchema { Type = "string", Format = "binary" }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
    }
}