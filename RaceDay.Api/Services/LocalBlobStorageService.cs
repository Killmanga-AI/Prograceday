using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RaceDay.Api.Services;

public static class MediaStoragePaths
{
    public const string RequestPath = "/media";
    public const string DefaultFolder = "App_Data/media";

    public static string ResolveRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Media:LocalPath"];
        var relative = string.IsNullOrWhiteSpace(configured) ? DefaultFolder : configured;
        return Path.IsPathRooted(relative)
            ? relative
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, relative));
    }
}

/// <summary>
/// Development storage provider that writes uploaded images to a local folder and serves
/// them from <see cref="MediaStoragePaths.RequestPath"/>. It mirrors the Azure provider's
/// validation and return contract so the MVC upload flow and tests run without credentials.
/// </summary>
public sealed class LocalBlobStorageService(
    IConfiguration configuration,
    IHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    ILogger<LocalBlobStorageService> logger) : IBlobStorageService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public async Task<string> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        contentType = contentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType))
            throw new ArgumentException("Only JPG, PNG and WebP images are accepted.", nameof(contentType));

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new ArgumentException("Unsupported image type.")
        };

        var root = MediaStoragePaths.ResolveRoot(configuration, environment);
        Directory.CreateDirectory(root);

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(root, blobName);

        await using (var target = new FileStream(
                         destination,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         81920,
                         useAsync: true))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        logger.LogInformation("Stored uploaded image as {BlobName} using local media storage.", blobName);

        var relativePath = $"{MediaStoragePaths.RequestPath.TrimEnd('/')}/{blobName}";
        var publicBaseUrl = configuration["Media:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            return $"{publicBaseUrl.TrimEnd('/')}{relativePath}";

        // Direct API calls (for example from Swagger) get an absolute URL based on the
        // current request. Containers should set Media:PublicBaseUrl because the MVC
        // server calls the API over the internal service hostname.
        var request = httpContextAccessor.HttpContext?.Request;
        return request is null
            ? relativePath
            : $"{request.Scheme}://{request.Host}{request.PathBase}{relativePath}";
    }
}
