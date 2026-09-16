using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Storage;

public class AzureBlobStorageService(
    IConfiguration configuration,
    ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct = default)
    {
        var connectionString = configuration["Azure:BlobConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobConnectionString is not configured.");
        var containerName = configuration["Azure:BlobContainer"] ?? "invoices";

        var client = new BlobContainerClient(connectionString, containerName);
        await client.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blob = client.GetBlobClient(blobName);

        if (file.CanSeek)
            file.Position = 0;

        await blob.UploadAsync(file, new BlobHttpHeaders
        {
            ContentType = GuessContentType(fileName)
        }, cancellationToken: ct);

        logger.LogInformation("Uploaded invoice to Azure Blob {Uri}", blob.Uri);
        return blob.Uri.ToString();
    }

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" => "image/tiff",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
}
