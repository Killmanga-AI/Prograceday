using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public sealed class EventsController(RaceDayContext db) : ApiControllerBase
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? type, [FromQuery] DateTime? from)
    {
        var query = db.Events.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.EventName.Contains(search) || x.Location.Contains(search));
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(x => x.EventType == type);
        if (from.HasValue)
            query = query.Where(x => x.EventDate >= from.Value);
        var events = await query.OrderBy(x => x.EventDate)
            .Select(x => new { x.EventId, x.OrganiserId, x.EventName, x.Description, x.EventDate, x.Location,
                x.RouteInfo, x.EventDistanceKm, x.EventType, x.BannerImageUrl, x.CreatedAt })
            .ToListAsync();
        return Ok(events);
    }

    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await db.Events.AsNoTracking().Where(x => x.EventId == id)
            .Select(x => new { x.EventId, x.OrganiserId, x.EventName, x.Description, x.EventDate, x.Location,
                x.RouteInfo, x.EventDistanceKm, x.EventType, x.BannerImageUrl, x.CreatedAt })
            .SingleOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize(Roles = "Organiser"), HttpPost]
    public async Task<IActionResult> Create(EventRequest request)
    {
        var item = new Event { OrganiserId = CurrentUserId, EventName = request.EventName.Trim(),
            Description = request.Description, EventDate = request.EventDate, Location = request.Location.Trim(),
            RouteInfo = request.RouteInfo, EventDistanceKm = request.EventDistanceKm, EventType = request.EventType };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.EventId }, ToResponse(item));
    }

    [Authorize(Roles = "Organiser"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();
        item.EventName = request.EventName.Trim(); item.Description = request.Description;
        item.EventDate = request.EventDate; item.Location = request.Location.Trim();
        item.RouteInfo = request.RouteInfo; item.EventDistanceKm = request.EventDistanceKm; item.EventType = request.EventType;
        await db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    [Authorize(Roles = "Organiser"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();
        var categories = await db.Categories.Where(x => x.EventId == id).ToListAsync();
        var categoryIds = categories.Select(x => x.CategoryId).ToList();
        var enrolments = await db.Enrolments.Where(x => categoryIds.Contains(x.CategoryId)).ToListAsync();
        var enrolmentIds = enrolments.Select(x => x.EnrolmentId).ToList();
        db.Results.RemoveRange(await db.Results.Where(x => enrolmentIds.Contains(x.EnrolmentId)).ToListAsync());
        db.Enrolments.RemoveRange(enrolments); db.Categories.RemoveRange(categories); db.Events.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [AllowAnonymous, HttpGet("{id:int}/categories")]
    public async Task<IActionResult> Categories(int id)
    {
        if (!await db.Events.AnyAsync(x => x.EventId == id)) return NotFound();
        return Ok(await db.Categories.AsNoTracking().Where(x => x.EventId == id)
            .Select(x => new { x.CategoryId, x.EventId, x.CategoryName, x.DistanceKm, x.MaxParticipants,
                EnrolledCount = x.Enrolments.Count })
            .ToListAsync());
    }

    [Authorize(Roles = "Organiser"), HttpGet("{id:int}/enrolments")]
    public async Task<IActionResult> Enrolments(int id)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();
        return Ok(await db.Enrolments.Where(x => x.Category!.EventId == id)
            .Select(x => new { x.EnrolmentId, x.ParticipantId, ParticipantName = x.Participant!.FullName,
                ParticipantEmail = x.Participant!.Email, x.CategoryId, CategoryName = x.Category!.CategoryName,
                x.EnrolmentDate, x.Status, HasResult = x.Result != null })
            .OrderBy(x => x.CategoryName).ThenBy(x => x.ParticipantName).ToListAsync());
    }

    [Authorize, HttpGet("{id:int}/results")]
    public async Task<IActionResult> Results(int id)
    {
        if (!await db.Events.AnyAsync(x => x.EventId == id)) return NotFound();
        return Ok(await db.Results.Where(x => x.Enrolment!.Category!.EventId == id)
            .OrderBy(x => x.Position).Select(x => new { x.ResultId, x.EnrolmentId,
                Participant = x.Enrolment!.Participant!.FullName, Category = x.Enrolment!.Category!.CategoryName,
                x.FinishTime, x.Position, EventDate = x.Enrolment!.Category!.Event!.EventDate })
            .ToListAsync());
    }

    private static object ToResponse(Event x) => new { x.EventId, x.OrganiserId, x.EventName, x.Description,
        x.EventDate, x.Location, x.RouteInfo, x.EventDistanceKm, x.EventType, x.BannerImageUrl, x.CreatedAt };
}
