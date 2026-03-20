using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace InsuranceEngine.Api.Security;

/// <summary>
/// Lightweight RBAC guard that checks the X-Role header for allowed roles.
/// This is intentionally header-driven to work with the existing mock auth setup.
/// </summary>
public sealed class RequireRoleHeaderAttribute : ActionFilterAttribute
{
    private readonly HashSet<string> _roles;

    public RequireRoleHeaderAttribute(params string[] roles)
    {
        _roles = roles.Select(r => r.Trim()).Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.ToLowerInvariant()).ToHashSet();
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (_roles.Count == 0)
        {
            base.OnActionExecuting(context);
            return;
        }

        var headerRole = context.HttpContext.Request.Headers["X-Role"].FirstOrDefault();
        if (headerRole == null || !_roles.Contains(headerRole.ToLowerInvariant()))
        {
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RequireRoleHeaderAttribute>)) as ILogger;
            logger?.LogWarning("RequireRoleHeader forbid: path={Path} required={Required} provided={Provided}",
                context.HttpContext.Request.Path, string.Join(",", _roles), headerRole ?? "<missing>");
            context.Result = new ForbidResult();
            return;
        }

        var allowLogger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RequireRoleHeaderAttribute>)) as ILogger;
        allowLogger?.LogInformation("RequireRoleHeader allow: path={Path} role={Role}", context.HttpContext.Request.Path, headerRole);

        base.OnActionExecuting(context);
    }
}
