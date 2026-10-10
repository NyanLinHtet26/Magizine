using Magizine.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Magizine.Shared.Services;

/// <summary>
/// Production-hardened local filesystem implementation of <see cref="IFileStorageService"/>.
/// Validates image magic bytes, enforces size limits, and uses 26-character ULID filenames
/// to prevent collisions and path traversal attacks.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "articles",
        "authors",
        "ads"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly FileStorageOptions _options;
    private readonly string _uploadRootPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment env,
        ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _uploadRootPath = _options.ResolveUploadRootPath(env.ContentRootPath);
        _logger = logger;
    }

    public async Task<Result<string>> SavePhotoAsync(
        IFormFile file,
        string folder,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return Result<string>.Error("ME#400", "Uploaded photo file is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            var maxMb = _options.MaxFileSizeBytes / (1024 * 1024);
            return Result<string>.Error("ME#400", $"Photo file exceeds the maximum allowed size of {maxMb} MB.");
        }

        var normalizedFolder = folder.Trim().ToLowerInvariant();
        if (!AllowedFolders.Contains(normalizedFolder))
        {
            return Result<string>.Error("ME#400", $"Invalid media folder '{folder}'.");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedExtensions.Contains(extension))
        {
            return Result<string>.Error("ME#400", "Only .jpg, .jpeg, .png, and .webp image files are allowed.");
        }

        await using var inputStream = file.OpenReadStream();
        if (!await HasValidImageSignatureAsync(inputStream, extension, ct))
        {
            return Result<string>.Error("ME#400", "File binary signature does not match a valid JPEG, PNG, or WebP image.");
        }

        inputStream.Position = 0;

        var targetFolderPath = Path.Combine(_uploadRootPath, normalizedFolder);
        Directory.CreateDirectory(targetFolderPath);

        var fileName = $"{Ulid.NewUlid()}{extension}";
        var physicalFilePath = Path.Combine(targetFolderPath, fileName);

        try
        {
            await using var fileStream = new FileStream(
                physicalFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await inputStream.CopyToAsync(fileStream, ct);
        }
        catch (Exception ex)
        {
            if (File.Exists(physicalFilePath))
            {
                try { File.Delete(physicalFilePath); } catch { /* ignore cleanup failure */ }
            }

            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Failed to save uploaded photo to disk."));
        }

        var requestPrefix = _options.RequestPath.TrimEnd('/');
        var relativeUrl = $"{requestPrefix}/{normalizedFolder}/{fileName}";
        return Result<string>.Success(relativeUrl);
    }

    public Task DeletePhotoAsync(string? photoUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            return Task.CompletedTask;
        }

        var requestPrefix = _options.RequestPath.TrimEnd('/') + "/";
        if (!photoUrl.StartsWith(requestPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // External URL (e.g., https://images.unsplash.com/...) — nothing to delete locally
            return Task.CompletedTask;
        }

        var relativeSubPath = photoUrl[requestPrefix.Length..]
            .Replace('/', Path.DirectorySeparatorChar);

        var fullTargetPath = Path.GetFullPath(Path.Combine(_uploadRootPath, relativeSubPath));
        var normalizedRoot = Path.GetFullPath(_uploadRootPath) + Path.DirectorySeparatorChar;

        // Guard against directory traversal
        if (!fullTargetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked attempt to delete path outside upload root: {PhotoUrl}", photoUrl);
            return Task.CompletedTask;
        }

        try
        {
            if (File.Exists(fullTargetPath))
            {
                File.Delete(fullTargetPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete physical photo file at {Path}", fullTargetPath);
        }

        return Task.CompletedTask;
    }

    public async Task DeletePhotosAsync(IEnumerable<string> photoUrls, CancellationToken ct = default)
    {
        foreach (var url in photoUrls)
        {
            await DeletePhotoAsync(url, ct);
        }
    }

    private static async Task<bool> HasValidImageSignatureAsync(Stream stream, string extension, CancellationToken ct)
    {
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (bytesRead < 4)
        {
            return false;
        }

        return extension switch
        {
            // JPEG: FF D8 FF
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            ".png" => bytesRead >= 8
                      && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                      && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,

            // WebP: "RIFF" (52 49 46 46) at 0..3 and "WEBP" (57 45 42 50) at 8..11
            ".webp" => bytesRead >= 12
                       && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                       && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,

            _ => false
        };
    }
}
