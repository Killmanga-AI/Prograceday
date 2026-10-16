using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.Api.Data; using RaceDay.Api.Dtos; using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;
[Authorize,Route("api/categories")]
public sealed class CategoriesController(RaceDayContext db):ApiControllerBase {
 /// <summary>Adds a category to an event owned by the organiser.</summary>
 [Authorize(Roles="Organiser"),HttpPost("/api/events/{eventId:int}/categories")] public async Task<IActionResult> Create(int eventId,CategoryRequest r){var e=await db.Events.FindAsync(eventId);if(e is null)return NotFound();if(e.OrganiserId!=CurrentUserId)return Forbid();var c=new Category{EventId=eventId,CategoryName=r.CategoryName.Trim(),DistanceKm=r.DistanceKm,MaxParticipants=r.MaxParticipants};db.Categories.Add(c);await db.SaveChangesAsync();return Created($"/api/categories/{c.CategoryId}",c);}
 /// <summary>Updates a category belonging to an event owned by the organiser.</summary>
 [Authorize(Roles="Organiser"),HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,CategoryRequest r){var c=await db.Categories.Include(x=>x.Event).SingleOrDefaultAsync(x=>x.CategoryId==id);if(c is null)return NotFound();if(c.Event!.OrganiserId!=CurrentUserId)return Forbid();c.CategoryName=r.CategoryName.Trim();c.DistanceKm=r.DistanceKm;c.MaxParticipants=r.MaxParticipants;await db.SaveChangesAsync();return Ok(c);}
 /// <summary>Deletes a category owned by the organiser if it has no enrolments.</summary>
 [Authorize(Roles="Organiser"),HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){var c=await db.Categories.Include(x=>x.Event).Include(x=>x.Enrolments).SingleOrDefaultAsync(x=>x.CategoryId==id);if(c is null)return NotFound();if(c.Event!.OrganiserId!=CurrentUserId)return Forbid();if(c.Enrolments.Count>0)return Conflict(new{message="Cannot delete a category with existing enrolments."});db.Categories.Remove(c);await db.SaveChangesAsync();return NoContent();}
}
