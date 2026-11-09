using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters;

public sealed class SessionAuthorizeAttribute : TypeFilterAttribute
{
    public SessionAuthorizeAttribute(string? role = null) : base(typeof(SessionAuthorizeFilter))
    {
        Arguments = [role ?? ""];
    }
}
public sealed class SessionAuthorizeFilter(IHttpContextAccessor accessor, string role) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var session = accessor.HttpContext?.Session;
        if (string.IsNullOrWhiteSpace(session?.GetString("Token")))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }
        if (!string.IsNullOrWhiteSpace(role) && !string.Equals(session.GetString("Role"), role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            return;
        }
        await next();
    }
}
