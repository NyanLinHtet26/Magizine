using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Magizine.Shared.Services;

/// <summary>
/// DI registration and static file middleware helpers for shared media storage across
/// MagizineAdmin.Api, MagizineAuthor.Api, and MagizinePublic.Api.
/// </summary>
public static class FileStorageExtensions
{
    /// <summary>
    /// Maximum multipart form body size (60 MB) to support uploading a cover photo + up to 10 gallery photos.
    /// </summary>
    public const long MaxMultipartRequestSizeBytes = 60L * 1024 * 1024;

    public static IServiceCollection AddMagizineFileStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = MaxMultipartRequestSizeBytes;
        });

        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        return services;
    }

    /// <summary>
    /// Serves uploaded media files from the shared upload root at "/uploads" with
    /// immutable 1-year browser/CDN caching headers (safe because every file has a unique ULID name).
    /// </summary>
    public static WebApplication UseMagizineStaticFiles(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var uploadRoot = options.ResolveUploadRootPath(app.Environment.ContentRootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadRoot),
            RequestPath = options.RequestPath,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
            }
        });

        return app;
    }
}
