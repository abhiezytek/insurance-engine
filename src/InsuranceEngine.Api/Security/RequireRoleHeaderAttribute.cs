using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace InsuranceEngine.Api.Security;

/// <summary>
/// RBAC guard that checks the user's role from JWT claims first,
/// falling back to the X-Role header for backward compatibility.
/// JWT claims are the preferred and secure source of role information.
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

        var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<RequireRoleHeaderAttribute>)) as ILogger;

        // 1. Prefer role from JWT claims (secure — cannot be spoofed)
        var jwtRoles = context.HttpContext.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value.ToLowerInvariant())
            .ToList() ?? new List<string>();

        if (jwtRoles.Any(r => _roles.Contains(r)))
        {
            logger?.LogInformation("RequireRole allow (JWT): path={Path} role={Role}",
                context.HttpContext.Request.Path, string.Join(",", jwtRoles));
            base.OnActionExecuting(context);
            return;
        }

        // 2. Fall back to X-Role header for backward compatibility with legacy clients
        var headerRole = context.HttpContext.Request.Headers["X-Role"].FirstOrDefault();
        if (headerRole != null && _roles.Contains(headerRole.ToLowerInvariant()))
        {
            logger?.LogInformation("RequireRole allow (header): path={Path} role={Role}",
                context.HttpContext.Request.Path, headerRole);
            base.OnActionExecuting(context);
            return;
        }

        logger?.LogWarning("RequireRole forbid: path={Path} required={Required} jwt={JwtRoles} header={Header}",
            context.HttpContext.Request.Path, string.Join(",", _roles),
            jwtRoles.Count > 0 ? string.Join(",", jwtRoles) : "<none>",
            headerRole ?? "<missing>");
        context.Result = new ForbidResult();
    }
}
