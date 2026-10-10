namespace Magizine.Shared.Services;

/// <summary>
/// Configuration options for photo/media file storage.
/// Bound from the "FileStorage" configuration section.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Physical directory where uploaded files are stored when using the Local provider.
    /// Defaults to a shared "SharedUploads" directory at the solution root so all 3 APIs
    /// (Admin, Author, Public) access the same physical storage.
    /// </summary>
    public string? UploadPath { get; set; }

    /// <summary>
    /// URL path prefix exposed by the static file middleware (default: "/uploads").
    /// </summary>
    public string RequestPath { get; set; } = "/uploads";

    /// <summary>
    /// Maximum allowed size in bytes for a single uploaded image (default: 5 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum number of gallery photos allowed in a single Create/Update request (default: 10).
    /// </summary>
    public int MaxGalleryPhotosPerRequest { get; set; } = 10;

    /// <summary>
    /// Resolves the physical root path for uploads, creating it if it does not exist.
    /// </summary>
    public string ResolveUploadRootPath(string contentRootPath)
    {
        var path = !string.IsNullOrWhiteSpace(UploadPath)
            ? UploadPath
            : Path.GetFullPath(Path.Combine(contentRootPath, "..", "SharedUploads"));

        Directory.CreateDirectory(path);
        return path;
    }
}
