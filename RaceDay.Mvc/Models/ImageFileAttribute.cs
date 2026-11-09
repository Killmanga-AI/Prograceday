using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Models;

/// <summary>
/// Validates an uploaded image before it is sent to the API. This is a user-experience
/// guard only: the API independently checks the declared content type, the file signature
/// bytes and the size limit, so a forged request cannot bypass the real policy.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ImageFileAttribute : ValidationAttribute
{
    public const int DefaultMaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public int MaxBytes { get; init; } = DefaultMaxBytes;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;
        if (value is not IFormFile file) return new ValidationResult("Select a valid image file.");

        if (file.Length <= 0) return new ValidationResult("The selected image file is empty.");

        if (file.Length > MaxBytes)
        {
            var megabytes = MaxBytes / (1024 * 1024);
            return new ValidationResult($"The image must be {megabytes} MB or smaller.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
            return new ValidationResult("Only JPG, PNG and WebP images are accepted.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return new ValidationResult("The file name must end with .jpg, .jpeg, .png or .webp.");

        return ValidationResult.Success;
    }
}
