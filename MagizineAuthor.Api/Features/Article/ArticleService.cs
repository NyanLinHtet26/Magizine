using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Enums;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Models.RequestArticle;
using Magizine.Shared.Security;
using Magizine.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAuthor.Api.Features.Article;

public sealed class ArticleService
{
    private const int MaxGalleryPhotosPerRequest = 10;

    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly IFileStorageService _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ArticleService> _logger;

    private string AuthorizedAuthorId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value
        ?? throw new UnauthorizedAccessException("Authenticated AuthorId claim is missing.");

    public ArticleService(
        MagizineDbContext db,
        DapperService dapperService,
        IFileStorageService fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ArticleService> logger)
    {
        _db = db;
        _dapperService = dapperService;
        _fileStorage = fileStorage;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<PagedResult<RequestArticleResModel>>> GetMyArticlesAsync(RequestArticleListReqModel req, CancellationToken ct = default)
    {
        try
        {
            var pageReq = PageRequest.Create(req.Page, req.PageSize);

            var parameters = new
            {
                p_search_keyword = req.SearchKeyword,
                p_category_id = req.ArticleCategoryId,
                p_author_id = AuthorizedAuthorId, // STRICT OWNERSHIP FILTER
                p_status = NormalizeAuthorStatusFilter(req.Status),
                p_exclude_draft = false,
                p_page = pageReq.Page,
                p_page_size = pageReq.PageSize
            };

            var paged = await _dapperService.GetPagedListAsync<RequestArticleResModel>(
                "fn_get_request_article_list",
                parameters,
                "RequestArticleId",
                pageReq);

            return Result<PagedResult<RequestArticleResModel>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<RequestArticleResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving author articles via Dapper"));
        }
    }

    public async Task<Result<RequestArticleResModel>> GetMyArticleByIdAsync(RequestArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var requestArticle = await _dapperService.GetFirstOrDefaultAsync<RequestArticleResModel>(
                "fn_get_request_article_by_id",
                new
                {
                    p_request_id = req.RequestArticleId,
                    p_author_id = AuthorizedAuthorId,
                    p_exclude_draft = false
                });

            if (requestArticle == null)
            {
                return Result<RequestArticleResModel>.Error("Article not found or access denied.");
            }

            requestArticle.Photos = await _dapperService.GetListAsync<ArticlePhotoResModel>(
                "fn_get_article_photos_by_request_id",
                new { p_request_id = requestArticle.RequestArticleId });

            return Result<RequestArticleResModel>.Success(requestArticle);
        }
        catch (Exception ex)
        {
            return Result<RequestArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving author article via Dapper"));
        }
    }

    public async Task<Result<RequestArticleResModel>> CreateArticleAsync(CreateRequestArticleReqModel req, CancellationToken ct = default)
    {
        var newlySavedFiles = new List<string>();
        try
        {
            var normalizedStatus = NormalizeAuthorInputStatus(req.Status);
            if (normalizedStatus is null)
            {
                return Result<RequestArticleResModel>.Error("Status must be either 'Draft' or 'Submitted'.");
            }

            var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
            if (!isCategoryValid)
                return Result<RequestArticleResModel>.Error("Selected Category does not exist.");

            if (req.GalleryItems is { Count: > MaxGalleryPhotosPerRequest })
            {
                return Result<RequestArticleResModel>.Error($"Maximum {MaxGalleryPhotosPerRequest} gallery photos are allowed per request.");
            }

            var coverPhotoUrl = req.PhotoUrl;
            if (req.CoverPhoto is not null)
            {
                var coverUpload = await _fileStorage.SavePhotoAsync(req.CoverPhoto, "articles", ct);
                if (coverUpload.IsError)
                {
                    return Result<RequestArticleResModel>.Error(coverUpload.RespCode, coverUpload.RespDesp ?? "Invalid cover photo.");
                }

                coverPhotoUrl = coverUpload.Data!;
                newlySavedFiles.Add(coverPhotoUrl);
            }

            var slug = Slugify(req.Title);
            var isSlugTaken = await _db.TblRequestArticles.AnyAsync(r => r.Slug == slug && !r.IsDeleted, ct);
            if (isSlugTaken)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString()[..5]}";
            }

            var requestArticleId = Ulid.NewUlid().ToString();
            var now = DateTimeOffset.UtcNow;

            var requestArticle = new TblRequestArticle
            {
                RequestArticleId = requestArticleId,
                Title = req.Title.Trim(),
                SubTitle = req.SubTitle?.Trim(),
                Slug = slug,
                Body = req.Body,
                PhotoUrl = coverPhotoUrl,
                PhotoCaption = req.PhotoCaption,
                PhotoCredit = req.PhotoCredit,
                ArticleCategoryId = req.ArticleCategoryId,
                AuthorId = AuthorizedAuthorId,
                Status = normalizedStatus,
                SubmittedAt = normalizedStatus == EnumRequestArticleStatus.Submitted.ToDbString() ? now : null,
                CreatedAt = now
            };

            var photoEntities = new List<TblArticlePhoto>();
            if (req.GalleryItems is { Count: > 0 })
            {
                for (var i = 0; i < req.GalleryItems.Count; i++)
                {
                    var item = req.GalleryItems[i];
                    var photoUpload = await _fileStorage.SavePhotoAsync(item.File, "articles", ct);
                    if (photoUpload.IsError)
                    {
                        await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
                        return Result<RequestArticleResModel>.Error(photoUpload.RespCode, $"Gallery photo #{i + 1}: {photoUpload.RespDesp}");
                    }

                    newlySavedFiles.Add(photoUpload.Data!);
                    photoEntities.Add(new TblArticlePhoto
                    {
                        ArticlePhotoId = Ulid.NewUlid().ToString(),
                        RequestArticleId = requestArticleId,
                        PhotoUrl = photoUpload.Data!,
                        Caption = item.Caption?.Trim(),
                        Credit = item.Credit?.Trim(),
                        SortOrder = item.SortOrder > 0 ? item.SortOrder : i + 1,
                        CreatedAt = now
                    });
                }
            }

            _db.TblRequestArticles.Add(requestArticle);
            if (photoEntities.Count > 0)
            {
                _db.TblArticlePhotos.AddRange(photoEntities);
            }

            await _db.SaveChangesAsync(ct);

            return await GetMyArticleByIdAsync(new RequestArticleDetailReqModel { RequestArticleId = requestArticleId }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            if (newlySavedFiles.Count > 0)
            {
                await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
            }
            return Result<RequestArticleResModel>.Error("An article with this slug already exists.");
        }
        catch (Exception ex)
        {
            if (newlySavedFiles.Count > 0)
            {
                await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
            }
            return Result<RequestArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error creating article in Tbl_RequestArticle"));
        }
    }

    public async Task<Result<RequestArticleResModel>> UpdateArticleAsync(UpdateRequestArticleReqModel req, CancellationToken ct = default)
    {
        var newlySavedFiles = new List<string>();
        try
        {
            var requestArticle = await _db.TblRequestArticles
                .FirstOrDefaultAsync(r => r.RequestArticleId == req.RequestArticleId && r.AuthorId == AuthorizedAuthorId && !r.IsDeleted, ct);

            if (requestArticle == null)
            {
                return Result<RequestArticleResModel>.Error("Article not found or you do not have permission to edit it.");
            }

            // LOCK CHECK: Once Approved by Admin, the article is published in Tbl_Article and locked.
            if (requestArticle.Status == EnumRequestArticleStatus.Approved.ToDbString())
            {
                return Result<RequestArticleResModel>.Error("This article has already been approved and published, so it is locked and cannot be edited.");
            }

            string? targetStatus = null;
            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                targetStatus = NormalizeAuthorInputStatus(req.Status);
                if (targetStatus is null)
                {
                    return Result<RequestArticleResModel>.Error("Status must be either 'Draft' or 'Submitted'.");
                }
            }

            if (requestArticle.ArticleCategoryId != req.ArticleCategoryId)
            {
                var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
                if (!isCategoryValid) return Result<RequestArticleResModel>.Error("Selected Category does not exist.");
            }

            if (req.NewGalleryItems is { Count: > MaxGalleryPhotosPerRequest })
            {
                return Result<RequestArticleResModel>.Error($"Maximum {MaxGalleryPhotosPerRequest} gallery photos are allowed per request.");
            }

            var now = DateTimeOffset.UtcNow;

            if (req.CoverPhoto is not null)
            {
                var coverUpload = await _fileStorage.SavePhotoAsync(req.CoverPhoto, "articles", ct);
                if (coverUpload.IsError)
                {
                    return Result<RequestArticleResModel>.Error(coverUpload.RespCode, coverUpload.RespDesp ?? "Invalid cover photo.");
                }

                newlySavedFiles.Add(coverUpload.Data!);
                requestArticle.PhotoUrl = coverUpload.Data!;
            }
            else if (!string.IsNullOrWhiteSpace(req.PhotoUrl))
            {
                requestArticle.PhotoUrl = req.PhotoUrl;
            }

            if (req.RemoveArticlePhotoIds is { Count: > 0 })
            {
                var photosToRemove = await _db.TblArticlePhotos
                    .Where(p => p.RequestArticleId == requestArticle.RequestArticleId && req.RemoveArticlePhotoIds.Contains(p.ArticlePhotoId) && !p.IsDeleted)
                    .ToListAsync(ct);

                foreach (var photo in photosToRemove)
                {
                    photo.IsDeleted = true;
                    photo.DeletedAt = now;
                    photo.UpdatedAt = now;
                }
            }

            if (req.NewGalleryItems is { Count: > 0 })
            {
                for (var i = 0; i < req.NewGalleryItems.Count; i++)
                {
                    var item = req.NewGalleryItems[i];
                    var photoUpload = await _fileStorage.SavePhotoAsync(item.File, "articles", ct);
                    if (photoUpload.IsError)
                    {
                        await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
                        return Result<RequestArticleResModel>.Error(photoUpload.RespCode, $"New gallery photo #{i + 1}: {photoUpload.RespDesp}");
                    }

                    newlySavedFiles.Add(photoUpload.Data!);
                    _db.TblArticlePhotos.Add(new TblArticlePhoto
                    {
                        ArticlePhotoId = Ulid.NewUlid().ToString(),
                        RequestArticleId = requestArticle.RequestArticleId,
                        PhotoUrl = photoUpload.Data!,
                        Caption = item.Caption?.Trim(),
                        Credit = item.Credit?.Trim(),
                        SortOrder = item.SortOrder > 0 ? item.SortOrder : i + 1,
                        CreatedAt = now
                    });
                }
            }

            requestArticle.Title = req.Title.Trim();
            requestArticle.SubTitle = req.SubTitle?.Trim();
            requestArticle.Body = req.Body;
            requestArticle.PhotoCaption = req.PhotoCaption;
            requestArticle.PhotoCredit = req.PhotoCredit;
            requestArticle.ArticleCategoryId = req.ArticleCategoryId;

            if (targetStatus is not null)
            {
                if (targetStatus == EnumRequestArticleStatus.Submitted.ToDbString() &&
                    requestArticle.Status != EnumRequestArticleStatus.Submitted.ToDbString())
                {
                    requestArticle.SubmittedAt = now;
                }
                else if (targetStatus == EnumRequestArticleStatus.Draft.ToDbString())
                {
                    requestArticle.SubmittedAt = null;
                }

                requestArticle.Status = targetStatus;
            }

            requestArticle.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            return await GetMyArticleByIdAsync(new RequestArticleDetailReqModel { RequestArticleId = requestArticle.RequestArticleId }, ct);
        }
        catch (Exception ex)
        {
            if (newlySavedFiles.Count > 0)
            {
                await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
            }
            return Result<RequestArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating article in Tbl_RequestArticle"));
        }
    }

    public async Task<Result<string>> SubmitForReviewAsync(RequestArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var requestArticle = await _db.TblRequestArticles
                .FirstOrDefaultAsync(r => r.RequestArticleId == req.RequestArticleId && r.AuthorId == AuthorizedAuthorId && !r.IsDeleted, ct);

            if (requestArticle == null)
            {
                return Result<string>.Error("Article not found or you do not have permission.");
            }

            if (requestArticle.Status == EnumRequestArticleStatus.Approved.ToDbString())
            {
                return Result<string>.Error("This article has already been approved and published, so it is locked.");
            }

            var now = DateTimeOffset.UtcNow;
            requestArticle.Status = EnumRequestArticleStatus.Submitted.ToDbString();
            requestArticle.SubmittedAt = now;
            requestArticle.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Article successfully submitted for Admin review.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error submitting article for review"));
        }
    }

    public async Task<Result<string>> DeleteMyArticleAsync(RequestArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var requestArticle = await _db.TblRequestArticles
                .FirstOrDefaultAsync(r => r.RequestArticleId == req.RequestArticleId && r.AuthorId == AuthorizedAuthorId && !r.IsDeleted, ct);

            if (requestArticle == null)
            {
                return Result<string>.Error("Article not found or you do not have permission.");
            }

            if (requestArticle.Status == EnumRequestArticleStatus.Approved.ToDbString())
            {
                return Result<string>.Error("This article has already been approved and published, so it is locked and cannot be deleted.");
            }

            var now = DateTimeOffset.UtcNow;
            requestArticle.IsDeleted = true;
            requestArticle.DeletedAt = now;
            requestArticle.UpdatedAt = now;

            var galleryPhotos = await _db.TblArticlePhotos
                .Where(p => p.RequestArticleId == requestArticle.RequestArticleId && !p.IsDeleted)
                .ToListAsync(ct);

            foreach (var photo in galleryPhotos)
            {
                photo.IsDeleted = true;
                photo.DeletedAt = now;
                photo.UpdatedAt = now;
            }

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Article deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error deleting author article"));
        }
    }

    private static string? NormalizeAuthorInputStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return EnumRequestArticleStatus.Draft.ToDbString();

        var trimmed = status.Trim();
        if (trimmed.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            return EnumRequestArticleStatus.Draft.ToDbString();

        if (trimmed.Equals("Submit", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
            return EnumRequestArticleStatus.Submitted.ToDbString();

        return null;
    }

    private static string? NormalizeAuthorStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var trimmed = status.Trim();
        if (trimmed.Equals("Submit", StringComparison.OrdinalIgnoreCase))
            return EnumRequestArticleStatus.Submitted.ToDbString();

        return trimmed;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
           || (ex.InnerException?.Message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) == true)
           || (ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true);

    private static string Slugify(string input)
    {
        var result = new System.Text.StringBuilder(input.Length);
        bool prevWasHyphen = true;

        foreach (var c in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(c);
                prevWasHyphen = false;
            }
            else if (!prevWasHyphen)
            {
                result.Append('-');
                prevWasHyphen = true;
            }
        }

        while (result.Length > 0 && result[^1] == '-')
        {
            result.Remove(result.Length - 1, 1);
        }

        return result.Length == 0 ? "article" : result.ToString();
    }
}
