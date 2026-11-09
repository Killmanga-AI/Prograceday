using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Organiser")]
public sealed class OrganiserController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken = default)
    {
        var result = await api.GetAsync<DashboardDto>("api/organiser/dashboard", cancellationToken);
        ViewBag.Error = result.Success ? null : result.Error;
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Events(CancellationToken cancellationToken = default)
    {
        var result = await api.GetAsync<List<OrganiserEventDto>>("api/organiser/events", cancellationToken);
        ViewBag.Error = result.Success ? null : result.Error;
        return View(result.Data ?? []);
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await api.PostAsync<EventDto>(
            "api/events",
            new { model.EventName, model.Description, model.EventDate, model.Location, model.RouteInfo, model.EventDistanceKm, model.EventType },
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the event.");
            return View(model);
        }

        TempData["Success"] = "Event created. Add at least one category so participants can enrol.";

        if (model.Banner is not null)
        {
            var upload = await api.UploadAsync<MediaUploadResponse>(
                $"api/events/{result.Data.EventId}/banner",
                model.Banner,
                cancellationToken);
            if (!upload.Success)
                TempData["Error"] = $"Event created, but the banner upload failed: {upload.Error}";
        }

        return RedirectToAction(nameof(EventCategories), new { id = result.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id, CancellationToken cancellationToken = default)
    {
        if (!await OwnsEventAsync(id, cancellationToken)) return RedirectToAction(nameof(Events));

        var result = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!result.Success || result.Data is null) return NotFound();

        var item = result.Data;
        return View(new EventViewModel
        {
            EventId = item.EventId,
            EventName = item.EventName,
            Description = item.Description,
            EventDate = item.EventDate,
            Location = item.Location,
            RouteInfo = item.RouteInfo,
            EventDistanceKm = item.EventDistanceKm,
            EventType = item.EventType,
            BannerImageUrl = item.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await OwnsEventAsync(model.EventId, cancellationToken)) return RedirectToAction(nameof(Events));

        var result = await api.PutAsync<EventDto>(
            $"api/events/{model.EventId}",
            new { model.EventName, model.Description, model.EventDate, model.Location, model.RouteInfo, model.EventDistanceKm, model.EventType },
            cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The event could not be updated.");
            return View(model);
        }

        if (model.Banner is not null)
        {
            var upload = await api.UploadAsync<MediaUploadResponse>(
                $"api/events/{model.EventId}/banner",
                model.Banner,
                cancellationToken);
            TempData[upload.Success ? "Success" : "Error"] = upload.Success
                ? "Event and banner updated."
                : $"Event updated, but the banner upload failed: {upload.Error}";
        }
        else
        {
            TempData["Success"] = "Event updated.";
        }

        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken cancellationToken = default)
    {
        if (!await OwnsEventAsync(id, cancellationToken))
        {
            TempData["Error"] = "You can only delete your own events.";
            return RedirectToAction(nameof(Events));
        }

        var result = await api.DeleteAsync<bool>($"api/events/{id}", cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Event deleted."
            : result.Error ?? "The event could not be deleted.";

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EventCategories(int id, CancellationToken cancellationToken = default)
    {
        if (!await OwnsEventAsync(id, cancellationToken)) return RedirectToAction(nameof(Events));

        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!eventResult.Success || eventResult.Data is null) return NotFound();

        var categories = await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories", cancellationToken);
        ViewBag.Event = eventResult.Data;
        ViewBag.Error = categories.Success ? null : categories.Error;
        return View(categories.Data ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var messages = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message)));
            TempData["Error"] = string.IsNullOrWhiteSpace(messages)
                ? "Please provide a valid category."
                : messages;
            return RedirectToAction(nameof(EventCategories), new { id = model.EventId });
        }

        var result = await api.PostAsync<CategoryDto>(
            $"api/events/{model.EventId}/categories",
            new { model.CategoryName, model.DistanceKm, model.MaxParticipants },
            cancellationToken);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Category added."
            : result.Error ?? "The category could not be added.";

        return RedirectToAction(nameof(EventCategories), new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(CategoryViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Category details are invalid. Check the name, distance and capacity.";
            return RedirectToAction(nameof(EventCategories), new { id = model.EventId });
        }

        var result = await api.PutAsync<CategoryDto>(
            $"api/categories/{model.CategoryId}",
            new { model.CategoryName, model.DistanceKm, model.MaxParticipants },
            cancellationToken);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Category updated."
            : result.Error ?? "The category could not be updated.";

        return RedirectToAction(nameof(EventCategories), new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId, CancellationToken cancellationToken = default)
    {
        var result = await api.DeleteAsync<bool>($"api/categories/{id}", cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Category deleted."
            : result.Error ?? "The category could not be deleted.";

        return RedirectToAction(nameof(EventCategories), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments(int id, CancellationToken cancellationToken = default)
    {
        if (!await OwnsEventAsync(id, cancellationToken)) return RedirectToAction(nameof(Events));

        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!eventResult.Success || eventResult.Data is null) return NotFound();

        var result = await api.GetAsync<List<EventEnrolmentDto>>($"api/events/{id}/enrolments", cancellationToken);
        ViewBag.Event = eventResult.Data;
        ViewBag.Error = result.Success ? null : result.Error;
        return View(result.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Results(int id, CancellationToken cancellationToken = default)
    {
        if (!await OwnsEventAsync(id, cancellationToken)) return RedirectToAction(nameof(Events));

        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!eventResult.Success || eventResult.Data is null) return NotFound();

        var enrolments = await api.GetAsync<List<EventEnrolmentDto>>($"api/events/{id}/enrolments", cancellationToken);
        if (!enrolments.Success)
        {
            TempData["Error"] = enrolments.Error ?? "You cannot manage this event.";
            return RedirectToAction(nameof(Events));
        }

        var results = await api.GetAsync<List<OrganiserResultDto>>($"api/events/{id}/results", cancellationToken);
        ViewBag.Event = eventResult.Data;
        ViewBag.Enrolments = enrolments.Data ?? [];
        ViewBag.Error = results.Success ? null : results.Error;
        return View(results.Data ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResult(
        ResultViewModel model,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || model.EnrolmentId <= 0 || model.FinishTime <= TimeSpan.Zero)
        {
            TempData["Error"] = "Choose a participant and enter a finish time greater than zero.";
            return RedirectToAction(nameof(Results), new { id = eventId });
        }

        var result = await api.PostAsync<JsonElementResponse>(
            $"api/enrolments/{model.EnrolmentId}/results",
            new { model.FinishTime, model.Position },
            cancellationToken);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Result recorded."
            : result.Error ?? "The result could not be recorded.";

        return RedirectToAction(nameof(Results), new { id = eventId });
    }

    private async Task<bool> OwnsEventAsync(int eventId, CancellationToken cancellationToken)
    {
        if (eventId <= 0) return false;

        var mine = await api.GetAsync<List<OrganiserEventDto>>("api/organiser/events", cancellationToken);
        return mine.Success && mine.Data is not null && mine.Data.Any(item => item.EventId == eventId);
    }
}
