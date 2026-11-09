using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl=returnUrl; return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) { ViewBag.ReturnUrl=returnUrl; return View(model); }
        var result = await api.PostAsync<System.Text.Json.JsonElement>("api/auth/login", model);
        var data = result.Data;
        if (!result.Success || data.ValueKind != System.Text.Json.JsonValueKind.Object || !data.TryGetProperty("token",out var token))
        { ModelState.AddModelError("", result.Error ?? "Login failed."); return View(model); }
        HttpContext.Session.SetString("Token", token.GetString() ?? "");
        HttpContext.Session.SetString("UserId", data.GetProperty("userId").ToString());
        HttpContext.Session.SetString("FullName", data.GetProperty("fullName").GetString() ?? "");
        HttpContext.Session.SetString("Email", data.GetProperty("email").GetString() ?? "");
        HttpContext.Session.SetString("Role", data.GetProperty("role").GetString() ?? "");
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! :
            (HttpContext.Session.GetString("Role") == "Organiser" ? "/Organiser/Dashboard" : "/Events/Browse"));
    }
    [HttpGet] public IActionResult Register() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<System.Text.Json.JsonElement>("api/auth/register", model);
        if (!result.Success) { ModelState.AddModelError("",result.Error??"Registration failed."); return View(model); }
        TempData["Success"]="Account created. Please sign in.";
        return RedirectToAction(nameof(Login));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout() { HttpContext.Session.Clear(); return RedirectToAction("Index","Home"); }
}
