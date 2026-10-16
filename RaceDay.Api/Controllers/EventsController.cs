using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.Api.Data; using RaceDay.Api.Dtos; using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;
[Route("api/events")]
public sealed class EventsController(RaceDayContext db) : ApiControllerBase {
 /// <summary>Lists all events; authentication is required by the API policy.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [Authorize,HttpGet] public async Task<IActionResult> GetAll()=>Ok(await db.Events.AsNoTracking().Select(x=>new{x.EventId,x.OrganiserId,x.EventName,x.Description,x.EventDate,x.Location,x.RouteInfo,x.EventDistanceKm,x.EventType,x.CreatedAt}).ToListAsync());
 /// <summary>Returns details for one event.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [Authorize,HttpGet("{id:int}")] public async Task<IActionResult> Get(int id){var e=await db.Events.AsNoTracking().Where(x=>x.EventId==id).Select(x=>new{x.EventId,x.OrganiserId,x.EventName,x.Description,x.EventDate,x.Location,x.RouteInfo,x.EventDistanceKm,x.EventType,x.CreatedAt}).SingleOrDefaultAsync();return e is null?NotFound():Ok(e);}
 /// <summary>Creates an event owned by the authenticated organiser.</summary>
 [ProducesResponseType(StatusCodes.Status201Created)]
 [ProducesResponseType(StatusCodes.Status400BadRequest)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status403Forbidden)]
 [Authorize(Roles="Organiser"),HttpPost] public async Task<IActionResult> Create(EventRequest r){var e=new Event{OrganiserId=CurrentUserId,EventName=r.EventName.Trim(),Description=r.Description,EventDate=r.EventDate,Location=r.Location.Trim(),RouteInfo=r.RouteInfo,EventDistanceKm=r.EventDistanceKm,EventType=r.EventType};db.Events.Add(e);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=e.EventId},new{e.EventId,e.OrganiserId,e.EventName,e.Description,e.EventDate,e.Location,e.RouteInfo,e.EventDistanceKm,e.EventType,e.CreatedAt});}
 /// <summary>Updates an event only when the caller owns it.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status400BadRequest)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status403Forbidden)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [Authorize(Roles="Organiser"),HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,EventRequest r){var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();e.EventName=r.EventName.Trim();e.Description=r.Description;e.EventDate=r.EventDate;e.Location=r.Location.Trim();e.RouteInfo=r.RouteInfo;e.EventDistanceKm=r.EventDistanceKm;e.EventType=r.EventType;await db.SaveChangesAsync();return Ok(new{e.EventId,e.OrganiserId,e.EventName,e.Description,e.EventDate,e.Location,e.RouteInfo,e.EventDistanceKm,e.EventType,e.CreatedAt});}
 /// <summary>Deletes an event only when the caller owns it.</summary>
 [ProducesResponseType(StatusCodes.Status204NoContent)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status403Forbidden)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [Authorize(Roles="Organiser"),HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();var categories=await db.Categories.Where(x=>x.EventId==id).ToListAsync();var categoryIds=categories.Select(x=>x.CategoryId).ToList();var enrolments=await db.Enrolments.Where(x=>categoryIds.Contains(x.CategoryId)).ToListAsync();var enrolmentIds=enrolments.Select(x=>x.EnrolmentId).ToList();var results=await db.Results.Where(x=>enrolmentIds.Contains(x.EnrolmentId)).ToListAsync();db.Results.RemoveRange(results);db.Enrolments.RemoveRange(enrolments);db.Categories.RemoveRange(categories);db.Events.Remove(e);await db.SaveChangesAsync();return NoContent();}
 /// <summary>Lists categories for an event.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [Authorize,HttpGet("{id:int}/categories")] public async Task<IActionResult> Categories(int id){if(!await db.Events.AnyAsync(x=>x.EventId==id))return NotFound();return Ok(await db.Categories.AsNoTracking().Where(x=>x.EventId==id).Select(x=>new{x.CategoryId,x.EventId,x.CategoryName,x.DistanceKm,x.MaxParticipants}).ToListAsync());}
 /// <summary>Lists enrolments for an event owned by the current organiser.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status403Forbidden)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [Authorize(Roles="Organiser"),HttpGet("{id:int}/enrolments")] public async Task<IActionResult> Enrolments(int id){var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();return Ok(await db.Enrolments.Where(x=>x.Category!.EventId==id).Select(x=>new{x.EnrolmentId,x.ParticipantId,ParticipantName=x.Participant!.FullName,x.CategoryId,CategoryName=x.Category!.CategoryName,x.EnrolmentDate,x.Status}).ToListAsync());}
 /// <summary>Returns event leaderboard with participant name, category, finish time and position.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [Authorize,HttpGet("{id:int}/results")] public async Task<IActionResult> Results(int id){if(!await db.Events.AnyAsync(x=>x.EventId==id))return NotFound();return Ok(await db.Results.Where(x=>x.Enrolment!.Category!.EventId==id).OrderBy(x=>x.Position).Select(x=>new{x.ResultId,x.EnrolmentId,Participant=x.Enrolment!.Participant!.FullName,Category=x.Enrolment!.Category!.CategoryName,x.FinishTime,x.Position}).ToListAsync());}
}
