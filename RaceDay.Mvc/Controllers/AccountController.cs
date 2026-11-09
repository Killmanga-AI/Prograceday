using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (SessionAuth.IsAuthenticated(HttpContext)) return RedirectToStart();

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var result = await api.PostAsync<AuthResponse>(
            "api/auth/login",
            new { model.Email, model.Password },
            cancellationToken);

        if (!result.Success || result.Data is null || string.IsNullOrWhiteSpace(result.Data.Token))
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sign in failed. Check your email and password.");
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        SessionAuth.SignIn(HttpContext, result.Data);
        TempData["Success"] = $"Welcome back, {result.Data.FullName}.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToStart(result.Data.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (SessionAuth.IsAuthenticated(HttpContext)) return RedirectToStart();
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.Role is not ("Organiser" or "Participant"))
            ModelState.AddModelError(nameof(model.Role), "Choose either Organiser or Participant.");

        if (!ModelState.IsValid) return View(model);

        var result = await api.PostAsync<JsonElementResponse>(
            "api/auth/register",
            new { model.FullName, model.Email, model.Password, model.Role },
            cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Account created. Sign in with your new credentials.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        SessionAuth.SignOut(HttpContext);
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToStart(string? role = null)
    {
        role ??= SessionAuth.Role(HttpContext);
        return role switch
        {
            "Organiser" => RedirectToAction("Dashboard", "Organiser"),
            "Participant" => RedirectToAction("Browse", "Events"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}

/// <summary>
/// Placeholder for endpoints whose 2xx body is not needed. It keeps the API client
/// generic while avoiding untyped JsonElement handling in controllers.
/// </summary>
public sealed class JsonElementResponse
{
    public int UserId { get; set; }
    public string? Message { get; set; }
}
