using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAdmin.Api.Features.Article;

public sealed class ArticleService
{
    private const int MaxGalleryPhotosPerRequest = 10;

    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ArticleService> _logger;

    // TODO: Extract from HttpContext when Auth is fully active
    private string AuthorizedAdminId => "01JADMIN000000000000000001";

    public ArticleService(
        MagizineDbContext db,
        DapperService dapperService,
        IFileStorageService fileStorage,
        ILogger<ArticleService> logger)
    {
        _db = db;
        _dapperService = dapperService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ArticleResModel>>> GetArticleList(ArticleListReqModel req, CancellationToken ct = default)
    {
        try
        {
            var pageReq = PageRequest.Create(req.Page, req.PageSize);
            
            var parameters = new
            {
                p_search_keyword = req.SearchKeyword,
                p_category_id = req.ArticleCategoryId,
                p_author_id = req.AuthorId,
                p_status = req.Status,
                p_is_featured = req.IsFeatured,
                p_is_spotlight = req.IsSpotlight,
                p_page = pageReq.Page,
                p_page_size = pageReq.PageSize
            };

            var paged = await _dapperService.GetPagedListAsync<ArticleResModel>(
                "fn_get_article_list", 
                parameters, 
                "ArticleId", 
                pageReq);

            return Result<PagedResult<ArticleResModel>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ArticleResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving article list via Dapper"));
        }
    }

    public async Task<Result<ArticleResModel>> GetArticleById(ArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var article = await _dapperService.GetFirstOrDefaultAsync<ArticleResModel>(
                "fn_get_article_by_id", 
                new { p_article_id = req.ArticleId, p_author_id = (string?)null });

            if (article == null)
            {
                return Result<ArticleResModel>.Error("Article not found.");
            }

            article.Photos = await _dapperService.GetListAsync<ArticlePhotoResModel>(
                "fn_get_article_photos_by_article_id",
                new { p_article_id = article.ArticleId });

            return Result<ArticleResModel>.Success(article);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving article via Dapper"));
        }
    }

    public async Task<Result<ArticleResModel>> CreateArticle(CreateArticleReqModel req, CancellationToken ct = default)
    {
        var newlySavedFiles = new List<string>();
        try
        {
            var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
            if (!isCategoryValid)
                return Result<ArticleResModel>.Error("Selected Category does not exist.");

            var isAuthorValid = await _db.TblAuthors.AnyAsync(a => a.AuthorId == req.AuthorId && !a.IsDeleted, ct);
            if (!isAuthorValid)
                return Result<ArticleResModel>.Error("Selected Author does not exist.");

            if (req.GalleryItems is { Count: > MaxGalleryPhotosPerRequest })
            {
                return Result<ArticleResModel>.Error($"Maximum {MaxGalleryPhotosPerRequest} gallery photos are allowed per request.");
            }

            // 1. Save CoverPhoto if uploaded
            var coverPhotoUrl = req.PhotoUrl;
            if (req.CoverPhoto is not null)
            {
                var coverUpload = await _fileStorage.SavePhotoAsync(req.CoverPhoto, "articles", ct);
                if (coverUpload.IsError)
                {
                    return Result<ArticleResModel>.Error(coverUpload.RespCode, coverUpload.RespDesp ?? "Invalid cover photo.");
                }

                coverPhotoUrl = coverUpload.Data!;
                newlySavedFiles.Add(coverPhotoUrl);
            }

            var slug = Slugify(req.Title);
            var isSlugTaken = await _db.TblArticles.AnyAsync(a => a.Slug == slug && !a.IsDeleted, ct);
            if (isSlugTaken)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            var articleId = Ulid.NewUlid().ToString();
            var now = DateTimeOffset.UtcNow;

            var article = new TblArticle
            {
                ArticleId = articleId,
                Title = req.Title.Trim(),
                SubTitle = req.SubTitle?.Trim(),
                Slug = slug,
                Body = req.Body,
                PhotoUrl = coverPhotoUrl,
                PhotoCaption = req.PhotoCaption,
                PhotoCredit = req.PhotoCredit,
                ArticleCategoryId = req.ArticleCategoryId,
                AuthorId = req.AuthorId,
                Status = req.Status ?? "Draft",
                IsFeatured = req.IsFeatured,
                IsSpotlight = req.IsSpotlight,
                CreatedAt = now,
                PublishedAt = req.Status == "Published" ? now : null
            };

            // 2. Save structured GalleryItems if uploaded
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
                        return Result<ArticleResModel>.Error(photoUpload.RespCode, $"Gallery photo #{i + 1}: {photoUpload.RespDesp}");
                    }

                    newlySavedFiles.Add(photoUpload.Data!);
                    photoEntities.Add(new TblArticlePhoto
                    {
                        ArticlePhotoId = Ulid.NewUlid().ToString(),
                        ArticleId = articleId,
                        PhotoUrl = photoUpload.Data!,
                        Caption = item.Caption?.Trim(),
                        Credit = item.Credit?.Trim(),
                        SortOrder = item.SortOrder > 0 ? item.SortOrder : i + 1,
                        CreatedAt = now
                    });
                }
            }

            _db.TblArticles.Add(article);
            if (photoEntities.Count > 0)
            {
                _db.TblArticlePhotos.AddRange(photoEntities);
            }

            await _db.SaveChangesAsync(ct);

            return await GetArticleById(new ArticleDetailReqModel { ArticleId = article.ArticleId }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            if (newlySavedFiles.Count > 0)
            {
                await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
            }
            return Result<ArticleResModel>.Error("An article with this slug already exists.");
        }
        catch (Exception ex)
        {
            if (newlySavedFiles.Count > 0)
            {
                await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
            }
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error creating article"));
        }
    }

    public async Task<Result<ArticleResModel>> UpdateArticle(UpdateArticleReqModel req, CancellationToken ct = default)
    {
        var newlySavedFiles = new List<string>();
        try
        {
            var article = await _db.TblArticles
                .FirstOrDefaultAsync(a => a.ArticleId == req.ArticleId && !a.IsDeleted, ct);

            if (article == null)
            {
                return Result<ArticleResModel>.Error("Article not found.");
            }

            if (article.ArticleCategoryId != req.ArticleCategoryId)
            {
                var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
                if (!isCategoryValid) return Result<ArticleResModel>.Error("Selected Category does not exist.");
            }

            if (article.AuthorId != req.AuthorId)
            {
                var isAuthorValid = await _db.TblAuthors.AnyAsync(a => a.AuthorId == req.AuthorId && !a.IsDeleted, ct);
                if (!isAuthorValid) return Result<ArticleResModel>.Error("Selected Author does not exist.");
            }

            if (req.NewGalleryItems is { Count: > MaxGalleryPhotosPerRequest })
            {
                return Result<ArticleResModel>.Error($"Maximum {MaxGalleryPhotosPerRequest} gallery photos are allowed per request.");
            }

            var now = DateTimeOffset.UtcNow;

            // 1. Replace CoverPhoto if a new file is uploaded
            if (req.CoverPhoto is not null)
            {
                var coverUpload = await _fileStorage.SavePhotoAsync(req.CoverPhoto, "articles", ct);
                if (coverUpload.IsError)
                {
                    return Result<ArticleResModel>.Error(coverUpload.RespCode, coverUpload.RespDesp ?? "Invalid cover photo.");
                }

                newlySavedFiles.Add(coverUpload.Data!);
                article.PhotoUrl = coverUpload.Data!;
            }
            else if (!string.IsNullOrWhiteSpace(req.PhotoUrl))
            {
                article.PhotoUrl = req.PhotoUrl;
            }

            // 2. Soft-delete any removed gallery photos
            if (req.RemoveArticlePhotoIds is { Count: > 0 })
            {
                var photosToRemove = await _db.TblArticlePhotos
                    .Where(p => p.ArticleId == article.ArticleId && req.RemoveArticlePhotoIds.Contains(p.ArticlePhotoId) && !p.IsDeleted)
                    .ToListAsync(ct);

                foreach (var photo in photosToRemove)
                {
                    photo.IsDeleted = true;
                    photo.DeletedAt = now;
                    photo.DeletedByAdminId = AuthorizedAdminId;
                    photo.UpdatedAt = now;
                }
            }

            // 3. Save and append any NewGalleryItems
            if (req.NewGalleryItems is { Count: > 0 })
            {
                for (var i = 0; i < req.NewGalleryItems.Count; i++)
                {
                    var item = req.NewGalleryItems[i];
                    var photoUpload = await _fileStorage.SavePhotoAsync(item.File, "articles", ct);
                    if (photoUpload.IsError)
                    {
                        await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
                        return Result<ArticleResModel>.Error(photoUpload.RespCode, $"New gallery photo #{i + 1}: {photoUpload.RespDesp}");
                    }

                    newlySavedFiles.Add(photoUpload.Data!);
                    _db.TblArticlePhotos.Add(new TblArticlePhoto
                    {
                        ArticlePhotoId = Ulid.NewUlid().ToString(),
                        ArticleId = article.ArticleId,
                        PhotoUrl = photoUpload.Data!,
                        Caption = item.Caption?.Trim(),
                        Credit = item.Credit?.Trim(),
                        SortOrder = item.SortOrder > 0 ? item.SortOrder : i + 1,
                        CreatedAt = now
                    });
                }
            }

            article.Title = req.Title.Trim();
            article.SubTitle = req.SubTitle?.Trim();
            article.Body = req.Body;
            article.PhotoCaption = req.PhotoCaption;
            article.PhotoCredit = req.PhotoCredit;
            article.ArticleCategoryId = req.ArticleCategoryId;
            article.AuthorId = req.AuthorId;
            article.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            return await GetArticleById(new ArticleDetailReqModel { ArticleId = article.ArticleId }, ct);
        }
        catch (Exception ex)
        {
            if (newlySavedFiles.Count > 0)
            {
                await _fileStorage.DeletePhotosAsync(newlySavedFiles, CancellationToken.None);
            }
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating article"));
        }
    }

    public async Task<Result<string>> ChangeArticleStatus(ChangeArticleStatusReqModel req, CancellationToken ct = default)
    {
        try
        {
            var article = await _db.TblArticles
                .FirstOrDefaultAsync(a => a.ArticleId == req.ArticleId && !a.IsDeleted, ct);

            if (article == null)
            {
                return Result<string>.Error("Article not found.");
            }

            if (req.Status == "Published" && article.Status != "Published")
            {
                article.PublishedAt = DateTimeOffset.UtcNow;
            }
            else if (req.Status != "Published")
            {
                article.PublishedAt = null;
            }

            article.Status = req.Status;
            article.IsFeatured = req.IsFeatured;
            article.IsSpotlight = req.IsSpotlight;
            article.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Article status updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating article status"));
        }
    }

    public async Task<Result<string>> DeleteArticle(ArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var article = await _db.TblArticles
                .FirstOrDefaultAsync(a => a.ArticleId == req.ArticleId && !a.IsDeleted, ct);

            if (article == null)
            {
                return Result<string>.Error("Article not found.");
            }

            var now = DateTimeOffset.UtcNow;
            article.IsDeleted = true;
            article.DeletedAt = now;

            var adminId = AuthorizedAdminId;
            if (!string.IsNullOrWhiteSpace(adminId))
            {
                article.DeletedByAdminId = adminId;
            }

            // Also soft-delete associated gallery photos
            var galleryPhotos = await _db.TblArticlePhotos
                .Where(p => p.ArticleId == article.ArticleId && !p.IsDeleted)
                .ToListAsync(ct);

            foreach (var photo in galleryPhotos)
            {
                photo.IsDeleted = true;
                photo.DeletedAt = now;
                photo.DeletedByAdminId = adminId;
            }

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Article deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error deleting article"));
        }
    }

    /// <summary>
    /// Permanently deletes physical files and database rows for soft-deleted article photos older than the retention window.
    /// </summary>
    public async Task<Result<int>> PurgeExpiredPhotosAsync(PurgeExpiredPhotosReqModel req, CancellationToken ct = default)
    {
        try
        {
            var retentionDays = req.RetentionDays > 0 ? req.RetentionDays : 30;
            var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

            var expiredPhotos = await _db.TblArticlePhotos
                .IgnoreQueryFilters()
                .Where(p => p.IsDeleted && p.DeletedAt != null && p.DeletedAt < cutoff)
                .ToListAsync(ct);

            if (expiredPhotos.Count == 0)
            {
                return Result<int>.Success(0);
            }

            var fileUrls = expiredPhotos.Select(p => p.PhotoUrl).ToList();
            await _fileStorage.DeletePhotosAsync(fileUrls, ct);

            _db.TblArticlePhotos.RemoveRange(expiredPhotos);
            await _db.SaveChangesAsync(ct);

            return Result<int>.Success(expiredPhotos.Count);
        }
        catch (Exception ex)
        {
            return Result<int>.Error(ex, "ME#999", e => _logger.LogError(e, "Error purging expired article photos"));
        }
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
