using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
public sealed class ProfileController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var result = await api.GetAsync<ProfileDto>("api/users/me", cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Your profile could not be loaded.";
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var update = await api.PutAsync<ProfileDto>(
            "api/users/me",
            new { model.FullName, model.Email },
            cancellationToken);

        if (!update.Success)
        {
            ModelState.AddModelError(string.Empty, update.Error ?? "Your profile could not be updated.");
            return View(model);
        }

        model.ProfilePictureUrl = update.Data?.ProfilePictureUrl;

        if (model.Picture is not null)
        {
            var upload = await api.UploadAsync<MediaUploadResponse>(
                "api/users/me/profile-picture",
                model.Picture,
                cancellationToken);

            if (!upload.Success)
            {
                ModelState.AddModelError(string.Empty, upload.Error ?? "The profile picture could not be uploaded.");
                return View(model);
            }

            model.ProfilePictureUrl = upload.Data?.ProfilePictureUrl;
        }

        HttpContext.Session.SetString(SessionKeys.FullName, model.FullName);
        HttpContext.Session.SetString(SessionKeys.Email, model.Email);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(ProfileDto profile) => new()
    {
        FullName = profile.FullName,
        Email = profile.Email,
        ProfilePictureUrl = profile.ProfilePictureUrl
    };
}
