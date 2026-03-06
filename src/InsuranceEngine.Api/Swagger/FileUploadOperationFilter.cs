using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InsuranceEngine.Api.Swagger;

/// <summary>
/// Replaces the default request body for endpoints marked with
/// <see cref="SwaggerFileUploadAttribute"/> with a multipart/form-data schema
/// that renders a file-picker in Swagger UI.
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFileUpload = context.MethodInfo
            .GetCustomAttributes(typeof(SwaggerFileUploadAttribute), inherit: true)
            .Any();

        if (!hasFileUpload) return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties =
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "Excel (.xlsx) or CSV (.csv) file to upload"
                            }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
    }
}
