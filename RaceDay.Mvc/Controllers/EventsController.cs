using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class EventsController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Browse(
        string? search,
        string? type,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["from"] = DateTime.UtcNow.Date.ToString("O")
        };
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search.Trim();
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;

        var result = await api.GetAsync<List<EventDto>>(
            QueryHelpers.AddQueryString("api/events", query),
            cancellationToken);

        ViewBag.Error = result.Success ? null : result.Error;
        ViewBag.Search = search;
        ViewBag.Type = type;
        return View(result.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();

        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!eventResult.Success || eventResult.Data is null)
        {
            if (eventResult.StatusCode == StatusCodes.Status404NotFound) return NotFound();
            ViewBag.Error = eventResult.Error;
            return View("Unavailable");
        }

        var categoryResult = await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories", cancellationToken);
        ViewBag.Categories = categoryResult.Data ?? [];
        ViewBag.Error = categoryResult.Success ? null : categoryResult.Error;
        return View(eventResult.Data);
    }

    [HttpPost, SessionAuthorize("Participant"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(
        int categoryId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0 || eventId <= 0)
        {
            TempData["Error"] = "Choose a valid category before enrolling.";
            return RedirectToAction(nameof(Browse));
        }

        var result = await api.PostAsync<JsonElementResponse>(
            $"api/categories/{categoryId}/enrolments",
            new { },
            cancellationToken);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "You are enrolled. Good luck!"
            : result.Error ?? "Enrolment failed.";

        return RedirectToAction(nameof(Details), new { id = eventId });
    }

    [HttpGet, SessionAuthorize("Participant")]
    public async Task<IActionResult> MyEvents(CancellationToken cancellationToken = default)
    {
        var result = await api.GetAsync<List<ParticipantEnrolmentDto>>("api/users/me/enrolments", cancellationToken);
        ViewBag.Error = result.Success ? null : result.Error;
        return View(result.Data ?? []);
    }

    [HttpPost, SessionAuthorize("Participant"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            TempData["Error"] = "That enrolment could not be found.";
            return RedirectToAction(nameof(MyEvents));
        }

        var result = await api.DeleteAsync<bool>($"api/enrolments/{id}", cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Enrolment cancelled."
            : result.Error ?? "The enrolment could not be cancelled.";

        return RedirectToAction(nameof(MyEvents));
    }

    [HttpGet, SessionAuthorize("Participant")]
    public async Task<IActionResult> MyResults(CancellationToken cancellationToken = default)
    {
        var result = await api.GetAsync<List<ParticipantResultDto>>("api/users/me/results", cancellationToken);
        ViewBag.Error = result.Success ? null : result.Error;
        return View(result.Data ?? []);
    }
}
