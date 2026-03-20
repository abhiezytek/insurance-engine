using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
            context.Result = new ForbidResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}
