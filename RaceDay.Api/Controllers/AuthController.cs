using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data; using RaceDay.Api.Dtos; using RaceDay.Api.Models; using RaceDay.Api.Services;
namespace RaceDay.Api.Controllers;
[Route("api/auth")]
public sealed class AuthController(RaceDayContext db, ITokenService tokens) : ApiControllerBase {
 private readonly PasswordHasher<User> _hasher = new();
 /// <summary>Registers an account as an Organiser or Participant. Public endpoint.</summary>
 [HttpPost("register")] public async Task<IActionResult> Register(RegisterRequest request) {
    if (request.Role != "Organiser" && request.Role != "Participant") {
        return BadRequest(new { message = "Role must be Organiser or Participant." });
    }

    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(x => x.Email == email)) {
        return Conflict(new { message = "Email is already registered." });
    }

    var role = await db.Roles.SingleOrDefaultAsync(x => x.RoleName == request.Role);
    if (role is null) {
        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The requested role is unavailable." });
    }

    var user = new User
    {
        FullName = request.FullName.Trim(),
        Email = email,
        RoleId = role.RoleId,
        Role = role,
        PasswordHash = string.Empty
    };

    user.PasswordHash = _hasher.HashPassword(user, request.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Created("/api/users/me", new { user.UserId, user.FullName, user.Email, Role = role.RoleName, user.CreatedAt });
 }
 /// <summary>Validates credentials and returns a short-lived bearer token.</summary>
 [HttpPost("login")] public async Task<IActionResult> Login(LoginRequest request) {
    var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant());
    if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed) {
        return Unauthorized(new { message = "Invalid email or password." });
    }

    return Ok(new { token = tokens.Create(user, user.Role!.RoleName), userId = user.UserId, fullName = user.FullName, email = user.Email, role = user.Role.RoleName, expiresInSeconds = 7200 });
 }
}
