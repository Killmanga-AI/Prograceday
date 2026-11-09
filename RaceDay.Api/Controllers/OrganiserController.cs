using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize(Roles = "Organiser"), Route("api/organiser")]
public sealed class OrganiserController(RaceDayContext db) : ControllerBase
{
    private int UserId => int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var events = db.Events.Where(x => x.OrganiserId == UserId);
        var eventIds = events.Select(x => x.EventId);
        var categoryIds = db.Categories.Where(x => eventIds.Contains(x.EventId)).Select(x => x.CategoryId);
        var enrolments = db.Enrolments.Where(x => categoryIds.Contains(x.CategoryId));
        var enrolmentIds = enrolments.Select(x => x.EnrolmentId);
        var now = DateTime.UtcNow;
        var summary = new
        {
            TotalEvents = await events.CountAsync(),
            UpcomingEvents = await events.CountAsync(x => x.EventDate >= now),
            PastEvents = await events.CountAsync(x => x.EventDate < now),
            TotalEnrolments = await enrolments.CountAsync(),
            ResultsRecorded = await db.Results.CountAsync(x => enrolmentIds.Contains(x.EnrolmentId)),
            RecentEnrolments = await enrolments.OrderByDescending(x => x.EnrolmentDate).Take(5)
                .Select(x => new { x.EnrolmentId, Participant = x.Participant!.FullName,
                    Event = x.Category!.Event!.EventName, Category = x.Category!.CategoryName,
                    x.EnrolmentDate, x.Status }).ToListAsync(),
            UpcomingEventList = await events.Where(x => x.EventDate >= now).OrderBy(x => x.EventDate).Take(5)
                .Select(x => new { x.EventId, x.EventName, x.EventDate, x.Location,
                    Enrolments = x.Categories.SelectMany(c => c.Enrolments).Count() }).ToListAsync()
        };
        return Ok(summary);
    }

    [HttpGet("events")]
    public async Task<IActionResult> MyEvents() => Ok(await db.Events.Where(x => x.OrganiserId == UserId)
        .OrderBy(x => x.EventDate).Select(x => new { x.EventId, x.EventName, x.EventDate, x.Location,
            x.EventType, x.EventDistanceKm, x.BannerImageUrl, Categories = x.Categories.Count,
            Enrolments = x.Categories.SelectMany(c => c.Enrolments).Count() }).ToListAsync());
}
