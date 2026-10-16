using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using RaceDay.Api.Data; using RaceDay.Api.Dtos;
namespace RaceDay.Api.Controllers;
[Authorize,Route("api/users")]
public sealed class UsersController(RaceDayContext db) : ApiControllerBase {
 /// <summary>Returns the authenticated user's profile.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [HttpGet("me")] public async Task<IActionResult> Me(){var u=await db.Users.Include(x=>x.Role).Where(x=>x.UserId==CurrentUserId).Select(x=>new{x.UserId,x.FullName,x.Email,Role=x.Role!.RoleName,x.CreatedAt}).SingleOrDefaultAsync();return u is null?NotFound():Ok(u);}
 /// <summary>Updates only the authenticated user's own profile.</summary>
 [ProducesResponseType(StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status400BadRequest)]
 [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
 [ProducesResponseType(StatusCodes.Status409Conflict)]
 [HttpPut("me")] public async Task<IActionResult> Update(UpdateProfileRequest request){var u=await db.Users.FindAsync(CurrentUserId);if(u is null)return NotFound();var email=request.Email.Trim().ToLowerInvariant();if(await db.Users.AnyAsync(x=>x.Email==email&&x.UserId!=u.UserId))return Conflict(new{message="Email is already in use."});u.FullName=request.FullName.Trim();u.Email=email;try{await db.SaveChangesAsync();}catch(DbUpdateException ex) when(DatabaseErrors.IsUniqueConstraintViolation(ex)){return Conflict(new{message="Email is already in use."});}return Ok(new{u.UserId,u.FullName,u.Email});}
}
