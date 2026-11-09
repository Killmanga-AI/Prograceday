using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Authorize]
[Route("api")]
public sealed class MediaController(
    RaceDayContext db,
    IBlobStorageService storage,
    ILogger<MediaController> logger) : ApiControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

    [Authorize(Roles = "Organiser")]
    [HttpPost("events/{id:int}/banner")]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadEventBanner(int id, IFormFile? file, CancellationToken cancellationToken)
    {
        var item = await db.Events.SingleOrDefaultAsync(x => x.EventId == id, cancellationToken);
        if (item is null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        var validation = await ValidateAsync(file);
        if (validation is not null) return BadRequest(new { message = validation });

        try
        {
            await using var stream = file!.OpenReadStream();
            item.BannerImageUrl = await storage.UploadImageAsync(stream, file.FileName, file.ContentType, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { item.EventId, item.BannerImageUrl });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Azure Blob Storage rejected the banner upload for event {EventId}.", id);
            return StatusCode(502, new { message = "Azure Blob Storage rejected the upload. Check the storage configuration and container permissions." });
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "The banner could not be written to local media storage for event {EventId}.", id);
            return StatusCode(500, new { message = "The image could not be stored. Please try again." });
        }
    }

    [HttpPost("users/me/profile-picture")]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? file, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserId == CurrentUserId, cancellationToken);
        if (user is null) return NotFound();

        var validation = await ValidateAsync(file);
        if (validation is not null) return BadRequest(new { message = validation });

        try
        {
            await using var stream = file!.OpenReadStream();
            user.ProfilePictureUrl = await storage.UploadImageAsync(stream, file.FileName, file.ContentType, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { user.UserId, user.ProfilePictureUrl });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Azure Blob Storage rejected the profile picture upload for user {UserId}.", user.UserId);
            return StatusCode(502, new { message = "Azure Blob Storage rejected the upload. Check the storage configuration and container permissions." });
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "The profile picture could not be written to local media storage for user {UserId}.", user.UserId);
            return StatusCode(500, new { message = "The image could not be stored. Please try again." });
        }
    }

    private static async Task<string?> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please select an image file.";
        if (file.Length > MaxImageBytes) return "Image must be 5 MB or smaller.";

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType)) return "Only JPG, PNG and WebP images are accepted.";

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        var valid = contentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            _ => false
        };
        return valid ? null : "The uploaded file contents do not match the selected image format.";
    }
}
