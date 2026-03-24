using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace InsuranceEngine.Api.Security;

/// <summary>
/// RBAC guard that checks the user's role from JWT claims.
/// The X-Role header fallback has been removed for production security.
/// All role information must come from the signed JWT token.
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

        // Log deprecation warning if any client still sends X-Role header
        if (context.HttpContext.Request.Headers.ContainsKey("X-Role"))
        {
            logger?.LogWarning(
                "Deprecated X-Role header used by {IP} on {Path}. Migrate to JWT Bearer token.",
                context.HttpContext.Connection.RemoteIpAddress,
                context.HttpContext.Request.Path);
        }

        // Check role from JWT claims only (secure — cannot be spoofed)
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

        logger?.LogWarning("RequireRole forbid: path={Path} required={Required} jwt={JwtRoles}",
            context.HttpContext.Request.Path, string.Join(",", _roles),
            jwtRoles.Count > 0 ? string.Join(",", jwtRoles) : "<none>");
        context.Result = new ForbidResult();
    }
}
