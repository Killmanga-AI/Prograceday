using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// When the API rejects a stored token with 401, the user is sent back to the sign-in
/// page instead of seeing a stale or broken page. The client records the expiry in
/// HttpContext.Items so this filter can act after the action has returned.
/// </summary>
public sealed class SessionExpiryFilter : IAsyncAlwaysRunResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var http = context.HttpContext;
        var expired = http.Items[SessionKeys.SessionExpired] is true;

        if (expired &&
            context.Result is not RedirectToActionResult and not RedirectResult &&
            !http.Request.Path.StartsWithSegments("/Account/Login"))
        {
            if (context.Controller is Controller controller)
                controller.TempData["Error"] = "Your session has expired. Please sign in again.";

            context.Result = new RedirectToActionResult(
                "Login",
                "Account",
                new { returnUrl = http.Request.Path + http.Request.QueryString });
        }

        await next();
    }
}
