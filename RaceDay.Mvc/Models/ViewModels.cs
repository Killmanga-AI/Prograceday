using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress, StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";
}

public sealed class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required, MinLength(8), StringLength(100), DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Choose either Organiser or Participant.")]
    [Display(Name = "Account type")]
    public string Role { get; set; } = "Participant";
}

public sealed class EventViewModel
{
    public int EventId { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Event date and time")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14).AddHours(7);

    [Required, StringLength(200)]
    [Display(Name = "Location")]
    public string Location { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Route information")]
    public string? RouteInfo { get; set; }

    [Range(typeof(decimal), "0.01", "9999.99", ErrorMessage = "Distance must be between 0.01 and 9999.99 km.")]
    [Display(Name = "Event distance (km)")]
    public decimal EventDistanceKm { get; set; } = 5;

    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Choose Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? BannerImageUrl { get; set; }

    [ImageFile]
    [Display(Name = "Event banner")]
    public IFormFile? Banner { get; set; }
}

public sealed class CategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; } = 5;

    [Range(1, 100000)]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;
}

public sealed class ResultViewModel
{
    public int EnrolmentId { get; set; }

    [Required]
    [Display(Name = "Finish time")]
    public TimeSpan FinishTime { get; set; }

    [Range(1, 1000000)]
    [Display(Name = "Position")]
    public int? Position { get; set; }
}

public sealed class ProfileViewModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    public string? ProfilePictureUrl { get; set; }

    [ImageFile]
    [Display(Name = "Profile picture")]
    public IFormFile? Picture { get; set; }
}

public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
}
