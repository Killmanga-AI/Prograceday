using System.ComponentModel.DataAnnotations;
namespace RaceDay.Api.Dtos;
public sealed record RegisterRequest([Required, StringLength(100,MinimumLength=2)] string FullName,[Required,EmailAddress, StringLength(150)] string Email,[Required,MinLength(8),StringLength(100)] string Password,[Required,RegularExpression("^(Organiser|Participant)$", ErrorMessage="Role must be Organiser or Participant.")] string Role);
public sealed record LoginRequest([Required,EmailAddress] string Email,[Required] string Password);
public sealed record UpdateProfileRequest([Required,StringLength(100,MinimumLength=2)] string FullName,[Required,EmailAddress,StringLength(150)] string Email);
public sealed record EventRequest([Required,StringLength(150,MinimumLength=2)] string EventName,[StringLength(1000)] string? Description,[Required,Range(typeof(DateTime),"1753-01-01","9999-12-31")] DateTime EventDate,[Required,StringLength(200)] string Location,[StringLength(500)] string? RouteInfo,[Range(typeof(decimal),"0.01","9999.99")] decimal EventDistanceKm,[Required,RegularExpression("^(Run|Walk|Cycle)$",ErrorMessage="Event type must be Run, Walk, or Cycle.")] string EventType);
public sealed record CategoryRequest([Required,StringLength(50)] string CategoryName,[Range(typeof(decimal),"0.01","999.99")] decimal DistanceKm,[Range(1,100000)] int MaxParticipants=100);
public sealed record ResultRequest([Required] TimeSpan FinishTime,[Range(1,1000000)] int? Position);
