using Magizine.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Magizine.Shared.Services;

/// <summary>
/// Provider-agnostic contract for storing and deleting uploaded media files.
/// Designed so <see cref="LocalFileStorageService"/> can be swapped for an S3,
/// Cloudflare R2, or Azure Blob Storage implementation without modifying controllers
/// or domain services.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Validates (extension, size, and binary magic-byte header) and saves an uploaded photo
    /// under a server-generated 26-character ULID filename.
    /// </summary>
    /// <param name="file">The uploaded form file.</param>
    /// <param name="folder">Target folder allowlist ("articles", "authors", "ads").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The relative URL path (e.g., "/uploads/articles/01J...webp") suitable for storing in NVARCHAR(300).</returns>
    Task<Result<string>> SavePhotoAsync(
        IFormFile file,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a single stored photo by its relative URL path. Safe to call with null/external URLs.
    /// </summary>
    Task DeletePhotoAsync(string? photoUrl, CancellationToken ct = default);

    /// <summary>
    /// Deletes multiple stored photos (used for compensating transaction rollback if a DB commit fails).
    /// </summary>
    Task DeletePhotosAsync(IEnumerable<string> photoUrls, CancellationToken ct = default);
}
