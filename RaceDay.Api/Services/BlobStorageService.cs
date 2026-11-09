using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

public sealed class AzureBlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        contentType = contentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType))
            throw new ArgumentException("Only JPG, PNG and WebP images are accepted.", nameof(contentType));

        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-media";
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString.");

        var service = new BlobServiceClient(connectionString);
        var container = service.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        await container.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new ArgumentException("Unsupported image type.")
        };
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }
}
