namespace InsuranceEngine.Api.Swagger;

/// <summary>
/// Marks an action method so that <see cref="FileUploadOperationFilter"/> replaces
/// its Swagger request body with a proper multipart/form-data file-picker.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SwaggerFileUploadAttribute : Attribute { }
