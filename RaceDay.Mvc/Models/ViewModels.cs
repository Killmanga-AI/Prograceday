using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
public sealed class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength=2)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
}
public sealed class EventViewModel
{
    public int EventId { get; set; }
    [Required, StringLength(150, MinimumLength=2)] public string EventName { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [Required, DataType(DataType.DateTime)] public DateTime EventDate { get; set; } = DateTime.Now.AddDays(14);
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [StringLength(500)] public string? RouteInfo { get; set; }
    [Range(typeof(decimal), "0.01", "9999.99")] public decimal EventDistanceKm { get; set; } = 5;
    [Required] public string EventType { get; set; } = "Run";
    public string? BannerImageUrl { get; set; }
    public IFormFile? Banner { get; set; }
}
public sealed class CategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    [Required] public string CategoryName { get; set; } = "";
    [Range(typeof(decimal), "0.01", "999.99")] public decimal DistanceKm { get; set; } = 5;
    [Range(1,100000)] public int MaxParticipants { get; set; } = 100;
}
public sealed class ResultViewModel
{
    public int EnrolmentId { get; set; }
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1,1000000)] public int? Position { get; set; }
}
public sealed class ProfileViewModel
{
    [Required, StringLength(100, MinimumLength=2)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
    public IFormFile? Picture { get; set; }
}
public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
}
public sealed class EventDto
{
    public int EventId { get; set; } public string EventName { get; set; } = ""; public string? Description { get; set; }
    public DateTime EventDate { get; set; } public string Location { get; set; } = ""; public string? RouteInfo { get; set; }
    public decimal EventDistanceKm { get; set; } public string EventType { get; set; } = ""; public string? BannerImageUrl { get; set; }
}
public sealed class CategoryDto
{
    public int CategoryId { get; set; } public int EventId { get; set; } public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; } public int MaxParticipants { get; set; } public int EnrolledCount { get; set; }
}
