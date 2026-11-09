namespace RaceDay.Mvc.Models;

/// <summary>Response returned by POST /api/auth/login.</summary>
public sealed class AuthResponse
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public int ExpiresInSeconds { get; set; }
}

public sealed class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

public sealed class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? RouteInfo { get; set; }
    public decimal EventDistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public int EnrolledCount { get; set; }
}

public sealed class DashboardDto
{
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int PastEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int ResultsRecorded { get; set; }
    public List<DashboardEnrolmentDto> RecentEnrolments { get; set; } = [];
    public List<DashboardEventDto> UpcomingEventList { get; set; } = [];
}

public sealed class DashboardEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public string Participant { get; set; } = "";
    public string Event { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}

public sealed class DashboardEventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public int Enrolments { get; set; }
}

public sealed class OrganiserEventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal EventDistanceKm { get; set; }
    public string? BannerImageUrl { get; set; }
    public int Categories { get; set; }
    public int Enrolments { get; set; }
}

public sealed class ParticipantEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int CategoryId { get; set; }
    public string Category { get; set; } = "";
    public int EventId { get; set; }
    public string Event { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string EventLocation { get; set; } = "";
    public string EventType { get; set; } = "";
    public string? EventBannerImageUrl { get; set; }
    public decimal DistanceKm { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
}

public sealed class EventEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string ParticipantEmail { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "";
    public bool HasResult { get; set; }
}

public sealed class OrganiserResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string Participant { get; set; } = "";
    public string Category { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime EventDate { get; set; }
}

public sealed class ParticipantResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string Event { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string EventLocation { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int TotalFinishers { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// <summary>Response returned by the banner and profile-picture upload endpoints.</summary>
public sealed class MediaUploadResponse
{
    public int? EventId { get; set; }
    public int? UserId { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
