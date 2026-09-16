using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Storage;

public class LocalBlobStorageService(
    IHostEnvironment environment,
    ILogger<LocalBlobStorageService> logger) : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct = default)
    {
        var uploads = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);

        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var path = Path.Combine(uploads, safeName);

        await using var fs = File.Create(path);
        if (file.CanSeek)
            file.Position = 0;
        await file.CopyToAsync(fs, ct);

        logger.LogInformation("Stored invoice locally at {Path}", path);
        return $"/uploads/{safeName}";
    }
}
