using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RaceDay.Mvc.Models;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class ImageFileAttributeTests
{
    private readonly ImageFileAttribute _attribute = new();

    [Fact]
    public void NoFile_IsValidBecauseUploadsAreOptional()
    {
        Assert.Equal(ValidationResult.Success, Validate(null));
    }

    [Fact]
    public void ValidPng_IsAccepted()
    {
        Assert.Equal(ValidationResult.Success, Validate(File("banner.png", "image/png", 1024)));
    }

    [Fact]
    public void Pdf_IsRejected()
    {
        var result = Validate(File("map.pdf", "application/pdf", 1024));
        Assert.NotNull(result);
        Assert.Contains("Only JPG, PNG and WebP", result!.ErrorMessage);
    }

    [Fact]
    public void OversizedFile_IsRejected()
    {
        var result = Validate(File("huge.png", "image/png", ImageFileAttribute.DefaultMaxBytes + 1));
        Assert.NotNull(result);
        Assert.Contains("5 MB or smaller", result!.ErrorMessage);
    }

    [Fact]
    public void WrongExtension_IsRejectedEvenWithAnImageContentType()
    {
        var result = Validate(File("banner.exe", "image/png", 1024));
        Assert.NotNull(result);
        Assert.Contains("file name must end with", result!.ErrorMessage);
    }

    [Fact]
    public void EmptyFile_IsRejected()
    {
        var result = Validate(File("banner.png", "image/png", 0));
        Assert.NotNull(result);
        Assert.Contains("empty", result!.ErrorMessage);
    }

    private ValidationResult? Validate(IFormFile? file) =>
        _attribute.GetValidationResult(file, new ValidationContext(new object()));

    private static IFormFile File(string name, string contentType, int length)
    {
        var stream = new MemoryStream(new byte[Math.Max(length, 1)]);
        return new FormFile(stream, 0, length, "Banner", name)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = contentType }
        };
    }
}
