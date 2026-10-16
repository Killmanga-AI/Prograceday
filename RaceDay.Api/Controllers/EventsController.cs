using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.Api.Data; using RaceDay.Api.Dtos; using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;
[Route("api/events")]
public sealed class EventsController(RaceDayContext db) : ApiControllerBase {
 /// <summary>Lists all events; authentication is required by the API policy.</summary>
 [Authorize,HttpGet] public async Task<IActionResult> GetAll()=>Ok(await db.Events.AsNoTracking().Select(x=>new{x.EventId,x.OrganiserId,x.EventName,x.Description,x.EventDate,x.Location,x.RouteInfo,x.CreatedAt}).ToListAsync());
 /// <summary>Returns details for one event.</summary>
 [Authorize,HttpGet("{id:int}")] public async Task<IActionResult> Get(int id){var e=await db.Events.AsNoTracking().Where(x=>x.EventId==id).Select(x=>new{x.EventId,x.OrganiserId,x.EventName,x.Description,x.EventDate,x.Location,x.RouteInfo,x.CreatedAt}).SingleOrDefaultAsync();return e is null?NotFound():Ok(e);}
 /// <summary>Creates an event owned by the authenticated organiser.</summary>
 [Authorize(Roles="Organiser"),HttpPost] public async Task<IActionResult> Create(EventRequest r){var e=new Event{OrganiserId=CurrentUserId,EventName=r.EventName.Trim(),Description=r.Description,EventDate=r.EventDate,Location=r.Location.Trim(),RouteInfo=r.RouteInfo};db.Events.Add(e);await db.SaveChangesAsync();return CreatedAtAction(nameof(Get),new{id=e.EventId},e);}
 /// <summary>Updates an event only when the caller owns it.</summary>
 [Authorize(Roles="Organiser"),HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,EventRequest r){var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();e.EventName=r.EventName.Trim();e.Description=r.Description;e.EventDate=r.EventDate;e.Location=r.Location.Trim();e.RouteInfo=r.RouteInfo;await db.SaveChangesAsync();return Ok(e);}
 /// <summary>Deletes an event only when the caller owns it.</summary>
 [Authorize(Roles="Organiser"),HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){var e=await db.Events.Include(x=>x.Categories).ThenInclude(x=>x.Enrolments).ThenInclude(x=>x.Result).SingleOrDefaultAsync(x=>x.EventId==id);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();db.Events.Remove(e);await db.SaveChangesAsync();return NoContent();}
 /// <summary>Lists categories for an event.</summary>
 [Authorize,HttpGet("{id:int}/categories")] public async Task<IActionResult> Categories(int id){if(!await db.Events.AnyAsync(x=>x.EventId==id))return NotFound();return Ok(await db.Categories.AsNoTracking().Where(x=>x.EventId==id).Select(x=>new{x.CategoryId,x.EventId,x.CategoryName,x.DistanceKm,x.MaxParticipants}).ToListAsync());}
 /// <summary>Lists enrolments for an event owned by the current organiser.</summary>
 [Authorize(Roles="Organiser"),HttpGet("{id:int}/enrolments")] public async Task<IActionResult> Enrolments(int id){var e=await db.Events.FindAsync(id);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();return Ok(await db.Enrolments.Where(x=>x.Category!.EventId==id).Select(x=>new{x.EnrolmentId,x.ParticipantId,ParticipantName=x.Participant!.FullName,x.CategoryId,CategoryName=x.Category!.CategoryName,x.EnrolmentDate,x.Status}).ToListAsync());}
 /// <summary>Returns event leaderboard with participant name, category, finish time and position.</summary>
 [Authorize,HttpGet("{id:int}/results")] public async Task<IActionResult> Results(int id){if(!await db.Events.AnyAsync(x=>x.EventId==id))return NotFound();return Ok(await db.Results.Where(x=>x.Enrolment!.Category!.EventId==id).OrderBy(x=>x.Position).Select(x=>new{x.ResultId,x.EnrolmentId,Participant=x.Enrolment!.Participant!.FullName,Category=x.Enrolment!.Category!.CategoryName,x.FinishTime,x.Position}).ToListAsync());}
}
