using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Authorize]
[Route("api")]
public sealed class EnrolmentsController(RaceDayContext db) : ApiControllerBase
{
    /// <summary>Enrols the authenticated participant into a category.</summary>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = "Participant")]
    [HttpPost("categories/{id:int}/enrolments")]
    public async Task<IActionResult> Create(int id)
    {
        await using var transaction = db.Database.IsSqlServer()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        if (db.Database.IsSqlServer())
        {
            // The row update serializes competing capacity checks until this transaction commits.
            await db.Categories
                .Where(category => category.CategoryId == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    category => category.MaxParticipants,
                    category => category.MaxParticipants));
        }

        var category = await db.Categories
            .Include(item => item.Enrolments)
            .SingleOrDefaultAsync(item => item.CategoryId == id);

        if (category is null)
        {
            return NotFound();
        }

        if (category.Enrolments.Any(enrolment => enrolment.ParticipantId == CurrentUserId))
        {
            return Conflict(new { message = "Already enrolled in this category." });
        }

        if (category.Enrolments.Count >= category.MaxParticipants)
        {
            return BadRequest(new { message = "Category is full." });
        }

        var enrolment = new Enrolment
        {
            CategoryId = id,
            ParticipantId = CurrentUserId
        };
        db.Enrolments.Add(enrolment);

        try
        {
            await db.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
        }
        catch (DbUpdateException exception) when (DatabaseErrors.IsUniqueConstraintViolation(exception))
        {
            return Conflict(new { message = "Already enrolled in this category." });
        }

        return Created(
            $"/api/enrolments/{enrolment.EnrolmentId}",
            new
            {
                enrolment.EnrolmentId,
                enrolment.ParticipantId,
                enrolment.CategoryId,
                enrolment.EnrolmentDate,
                enrolment.Status
            });
    }

    /// <summary>Lists the current participant's enrolments.</summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Roles = "Participant")]
    [HttpGet("users/me/enrolments")]
    public async Task<IActionResult> Mine()
    {
        var enrolments = await db.Enrolments
            .Where(enrolment => enrolment.ParticipantId == CurrentUserId)
            .Select(enrolment => new
            {
                enrolment.EnrolmentId,
                enrolment.CategoryId,
                Category = enrolment.Category!.CategoryName,
                Event = enrolment.Category!.Event!.EventName,
                enrolment.EnrolmentDate,
                enrolment.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Cancels the current participant's own enrolment.</summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = "Participant")]
    [HttpDelete("enrolments/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var enrolment = await db.Enrolments.FindAsync(id);
        if (enrolment is null)
        {
            return NotFound();
        }

        if (enrolment.ParticipantId != CurrentUserId)
        {
            return Forbid();
        }

        if (await db.Results.AnyAsync(result => result.EnrolmentId == id))
        {
            return Conflict(new { message = "An enrolment with a recorded result cannot be cancelled." });
        }

        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Captures a result for an enrolment belonging to the organiser.</summary>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = "Organiser")]
    [HttpPost("enrolments/{id:int}/results")]
    public async Task<IActionResult> AddResult(int id, ResultRequest request)
    {
        var enrolment = await db.Enrolments
            .Include(item => item.Category)
            .ThenInclude(category => category!.Event)
            .SingleOrDefaultAsync(item => item.EnrolmentId == id);

        if (enrolment is null)
        {
            return NotFound();
        }

        if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
        {
            return Forbid();
        }

        if (await db.Results.AnyAsync(result => result.EnrolmentId == id))
        {
            return Conflict(new { message = "A result has already been recorded." });
        }

        if (request.FinishTime <= TimeSpan.Zero || request.FinishTime >= TimeSpan.FromDays(1))
        {
            return BadRequest(new { message = "Finish time must be greater than zero and less than 24 hours." });
        }

        var result = new Result
        {
            EnrolmentId = id,
            FinishTime = request.FinishTime,
            Position = request.Position,
            RecordedByUserId = CurrentUserId
        };
        db.Results.Add(result);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (DatabaseErrors.IsUniqueConstraintViolation(exception))
        {
            return Conflict(new { message = "A result has already been recorded." });
        }

        return Created(
            $"/api/enrolments/{id}/results",
            new
            {
                result.ResultId,
                result.EnrolmentId,
                result.FinishTime,
                result.Position,
                result.RecordedByUserId,
                result.RecordedAt
            });
    }

    /// <summary>Returns results belonging to the current participant.</summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Roles = "Participant")]
    [HttpGet("users/me/results")]
    public async Task<IActionResult> MyResults()
    {
        var results = await db.Results
            .Where(result => result.Enrolment!.ParticipantId == CurrentUserId)
            .Select(result => new
            {
                result.ResultId,
                result.EnrolmentId,
                Event = result.Enrolment!.Category!.Event!.EventName,
                Category = result.Enrolment!.Category!.CategoryName,
                result.FinishTime,
                result.Position,
                result.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }
}
