using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Protects MVC actions with the server-side session. This is a usability guard:
/// every API call still carries the bearer token and the API independently enforces
/// authentication, roles and resource ownership.
/// </summary>
public sealed class SessionAuthorizeAttribute : TypeFilterAttribute
{
    public SessionAuthorizeAttribute(string? role = null) : base(typeof(SessionAuthorizeFilter))
    {
        Arguments = [role ?? ""];
    }
}

public sealed class SessionAuthorizeFilter(string role) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;

        if (!SessionAuth.IsAuthenticated(http))
        {
            if (http.Session.GetString(SessionKeys.Token) is not null && context.Controller is Controller controller)
                controller.TempData["Error"] = "Your session has expired. Please sign in again.";

            context.Result = new RedirectToActionResult(
                "Login",
                "Account",
                new { returnUrl = http.Request.Path + http.Request.QueryString });
            return;
        }

        if (!string.IsNullOrWhiteSpace(role) && !SessionAuth.IsInRole(http, role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            return;
        }

        await next();
    }
}
