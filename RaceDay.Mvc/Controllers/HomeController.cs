using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Mvc.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusPage(int code)
    {
        Response.StatusCode = code;
        ViewBag.Code = code;
        ViewBag.Message = code switch
        {
            StatusCodes.Status400BadRequest => "The request could not be understood. Check the details and try again.",
            StatusCodes.Status401Unauthorized => "Please sign in to continue.",
            StatusCodes.Status403Forbidden => "Your account does not have permission to view this page.",
            StatusCodes.Status404NotFound => "We could not find the page or record you were looking for.",
            StatusCodes.Status409Conflict => "That action conflicts with the current data. Refresh and try again.",
            StatusCodes.Status429TooManyRequests => "Too many requests. Wait a moment and try again.",
            _ => "Something went wrong while processing your request."
        };
        return View();
    }

    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
