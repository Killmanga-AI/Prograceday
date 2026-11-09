using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
public sealed class ProfileController(RaceDayApiClient api) : Controller
{
    [HttpGet] public async Task<IActionResult> Index()
    {
        var r=await api.GetAsync<System.Text.Json.JsonElement>("api/users/me");
        var d = r.Data;
        if(!r.Success || d.ValueKind != System.Text.Json.JsonValueKind.Object) { TempData["Error"]=r.Error; return RedirectToAction("Index","Home"); }
        return View(new ProfileViewModel{FullName=d.GetProperty("fullName").GetString()??"",Email=d.GetProperty("email").GetString()??"",
            ProfilePictureUrl=d.TryGetProperty("profilePictureUrl",out var p)?p.GetString():null});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if(!ModelState.IsValid) return View(model);
        var update=await api.PutAsync<System.Text.Json.JsonElement>("api/users/me",new{model.FullName,model.Email});
        if(!update.Success) { ModelState.AddModelError("",update.Error??"Profile update failed."); return View(model); }
        if(update.Data is { } updated && updated.TryGetProperty("profilePictureUrl",out var existingPicture))
            model.ProfilePictureUrl=existingPicture.GetString();
        if(model.Picture is not null)
        {
            var upload=await api.UploadAsync<System.Text.Json.JsonElement>("api/users/me/profile-picture",model.Picture);
            if(!upload.Success) { ModelState.AddModelError("",upload.Error??"Picture upload failed."); return View(model); }
            if(upload.Data is { } uploaded && uploaded.TryGetProperty("profilePictureUrl", out var pictureUrl))
                model.ProfilePictureUrl=pictureUrl.GetString();
        }
        HttpContext.Session.SetString("FullName",model.FullName); HttpContext.Session.SetString("Email",model.Email);
        TempData["Success"]="Profile updated."; return View(model);
    }
}
