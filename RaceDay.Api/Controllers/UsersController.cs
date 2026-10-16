using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.Api.Data; using RaceDay.Api.Dtos; using RaceDay.Api.Models;
namespace RaceDay.Api.Controllers;
[Authorize,Route("api/users")]
public sealed class UsersController(RaceDayContext db) : ApiControllerBase {
 /// <summary>Returns the authenticated user's profile.</summary>
 [HttpGet("me")] public async Task<IActionResult> Me(){var u=await db.Users.Include(x=>x.Role).Where(x=>x.UserId==CurrentUserId).Select(x=>new{x.UserId,x.FullName,x.Email,Role=x.Role!.RoleName,x.CreatedAt}).SingleOrDefaultAsync();return u is null?NotFound():Ok(u);}
 /// <summary>Updates only the authenticated user's own profile.</summary>
 [HttpPut("me")] public async Task<IActionResult> Update(UpdateProfileRequest request){var u=await db.Users.FindAsync(CurrentUserId);if(u is null)return NotFound();var email=request.Email.Trim().ToLowerInvariant();if(await db.Users.AnyAsync(x=>x.Email==email&&x.UserId!=u.UserId))return Conflict(new{message="Email is already in use."});u.FullName=request.FullName.Trim();u.Email=email;await db.SaveChangesAsync();return Ok(new{u.UserId,u.FullName,u.Email});}
}
