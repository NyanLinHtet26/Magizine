using FluentAssertions;
using Magizine.Shared.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Magizine.Tests.Features.Media;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempUploadRoot;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _tempUploadRoot = Path.Combine(Path.GetTempPath(), $"MagizineUploadsTest_{Ulid.NewUlid()}");
        Directory.CreateDirectory(_tempUploadRoot);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempUploadRoot);

        var options = Options.Create(new FileStorageOptions
        {
            UploadPath = _tempUploadRoot,
            RequestPath = "/uploads",
            MaxFileSizeBytes = 5 * 1024 * 1024
        });

        _sut = new LocalFileStorageService(
            options,
            envMock.Object,
            NullLogger<LocalFileStorageService>.Instance);
    }

    [Fact]
    public async Task SavePhotoAsync_WithValidJpegMagicBytes_SavesWithUlidFileName()
    {
        // Minimal valid JPEG header: FF D8 FF E0 ...
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        var formFile = CreateFormFile(jpegBytes, "cover.jpg", "image/jpeg");

        var result = await _sut.SavePhotoAsync(formFile, "articles");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().StartWith("/uploads/articles/");
        result.Data.Should().EndWith(".jpg");

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(result.Data);
        fileNameWithoutExt.Should().HaveLength(26);
        Ulid.TryParse(fileNameWithoutExt, out _).Should().BeTrue();

        var expectedPhysicalPath = Path.Combine(_tempUploadRoot, "articles", Path.GetFileName(result.Data!));
        File.Exists(expectedPhysicalPath).Should().BeTrue();
    }

    [Fact]
    public async Task SavePhotoAsync_WithDisguisedNonImageBytes_RejectsSignature()
    {
        // Text/script bytes disguised with a .jpg extension
        var fakeBytes = "<script>alert('xss')</script>"u8.ToArray();
        var formFile = CreateFormFile(fakeBytes, "disguised.jpg", "image/jpeg");

        var result = await _sut.SavePhotoAsync(formFile, "articles");

        result.IsError.Should().BeTrue();
        result.RespDesp.Should().Contain("signature");
    }

    [Fact]
    public async Task SavePhotoAsync_WithDisallowedFolder_ReturnsError()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var formFile = CreateFormFile(jpegBytes, "cover.jpg", "image/jpeg");

        var result = await _sut.SavePhotoAsync(formFile, "../system");

        result.IsError.Should().BeTrue();
        result.RespDesp.Should().Contain("Invalid media folder");
    }

    [Fact]
    public async Task DeletePhotosAsync_RemovesSavedPhysicalFiles_ForCompensatingRollback()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var formFile = CreateFormFile(pngBytes, "gallery.png", "image/png");

        var saved = await _sut.SavePhotoAsync(formFile, "articles");
        saved.IsSuccess.Should().BeTrue();

        var physicalPath = Path.Combine(_tempUploadRoot, "articles", Path.GetFileName(saved.Data!));
        File.Exists(physicalPath).Should().BeTrue();

        await _sut.DeletePhotosAsync([saved.Data!]);

        File.Exists(physicalPath).Should().BeFalse();
    }

    private static IFormFile CreateFormFile(byte[] bytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempUploadRoot))
        {
            try { Directory.Delete(_tempUploadRoot, recursive: true); } catch { /* ignore */ }
        }
    }
}
