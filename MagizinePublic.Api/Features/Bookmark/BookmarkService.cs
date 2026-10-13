using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Enums;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Models.Reader;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizinePublic.Api.Features.Bookmark;

public sealed class BookmarkService
{
    private readonly MagizineDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BookmarkService> _logger;

    public BookmarkService(
        MagizineDbContext db,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BookmarkService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string? AuthorizedReaderId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value;

    public async Task<Result<BookmarkToggleResModel>> ToggleBookmarkAsync(string articleId, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<BookmarkToggleResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required to bookmark articles.");
            }

            var article = await _db.TblArticles
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && a.Status == "Published", ct);

            if (article == null)
            {
                return Result<BookmarkToggleResModel>.Error(
                    JsonResource.Validation,
                    "Article not found or not published.");
            }

            var existing = await _db.TblReaderBookmarks
                .FirstOrDefaultAsync(b => b.ReaderId == readerId && b.ArticleId == articleId, ct);

            bool isBookmarked;
            string message;

            if (existing != null)
            {
                _db.TblReaderBookmarks.Remove(existing);
                await _db.SaveChangesAsync(ct);
                isBookmarked = false;
                message = "Article removed from bookmarks.";
            }
            else
            {
                var newBookmark = new TblReaderBookmark
                {
                    ReaderBookmarkId = Ulid.NewUlid().ToString(),
                    ReaderId = readerId,
                    ArticleId = articleId,
                    CreatedAt = _timeProvider.GetUtcNow()
                };

                _db.TblReaderBookmarks.Add(newBookmark);
                await _db.SaveChangesAsync(ct);
                isBookmarked = true;
                message = "Article bookmarked successfully.";
            }

            var count = await _db.TblReaderBookmarks
                .CountAsync(b => b.ArticleId == articleId, ct);

            return Result<BookmarkToggleResModel>.Success(new BookmarkToggleResModel
            {
                ArticleId = articleId,
                IsBookmarked = isBookmarked,
                BookmarkCount = count,
                Message = message
            });
        }
        catch (Exception ex)
        {
            return Result<BookmarkToggleResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error toggling bookmark for article '{ArticleId}'", articleId));
        }
    }

    public async Task<Result<BookmarkToggleResModel>> AddBookmarkAsync(string articleId, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<BookmarkToggleResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required to bookmark articles.");
            }

            var article = await _db.TblArticles
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && a.Status == "Published", ct);

            if (article == null)
            {
                return Result<BookmarkToggleResModel>.Error(
                    JsonResource.Validation,
                    "Article not found or not published.");
            }

            var existing = await _db.TblReaderBookmarks
                .FirstOrDefaultAsync(b => b.ReaderId == readerId && b.ArticleId == articleId, ct);

            if (existing == null)
            {
                var newBookmark = new TblReaderBookmark
                {
                    ReaderBookmarkId = Ulid.NewUlid().ToString(),
                    ReaderId = readerId,
                    ArticleId = articleId,
                    CreatedAt = _timeProvider.GetUtcNow()
                };

                _db.TblReaderBookmarks.Add(newBookmark);
                await _db.SaveChangesAsync(ct);
            }

            var count = await _db.TblReaderBookmarks
                .CountAsync(b => b.ArticleId == articleId, ct);

            return Result<BookmarkToggleResModel>.Success(new BookmarkToggleResModel
            {
                ArticleId = articleId,
                IsBookmarked = true,
                BookmarkCount = count,
                Message = "Article bookmarked successfully."
            });
        }
        catch (Exception ex)
        {
            return Result<BookmarkToggleResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error adding bookmark for article '{ArticleId}'", articleId));
        }
    }

    public async Task<Result<BookmarkToggleResModel>> RemoveBookmarkAsync(string articleId, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<BookmarkToggleResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required to manage bookmarks.");
            }

            var existing = await _db.TblReaderBookmarks
                .FirstOrDefaultAsync(b => b.ReaderId == readerId && b.ArticleId == articleId, ct);

            if (existing != null)
            {
                _db.TblReaderBookmarks.Remove(existing);
                await _db.SaveChangesAsync(ct);
            }

            var count = await _db.TblReaderBookmarks
                .CountAsync(b => b.ArticleId == articleId, ct);

            return Result<BookmarkToggleResModel>.Success(new BookmarkToggleResModel
            {
                ArticleId = articleId,
                IsBookmarked = false,
                BookmarkCount = count,
                Message = "Article removed from bookmarks."
            });
        }
        catch (Exception ex)
        {
            return Result<BookmarkToggleResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error removing bookmark for article '{ArticleId}'", articleId));
        }
    }

    public async Task<Result<BookmarkCheckResModel>> IsArticleBookmarkedAsync(string articleId, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<BookmarkCheckResModel>.Success(new BookmarkCheckResModel
                {
                    ArticleId = articleId,
                    IsBookmarked = false
                });
            }

            var isBookmarked = await _db.TblReaderBookmarks
                .AnyAsync(b => b.ReaderId == readerId && b.ArticleId == articleId, ct);

            return Result<BookmarkCheckResModel>.Success(new BookmarkCheckResModel
            {
                ArticleId = articleId,
                IsBookmarked = isBookmarked
            });
        }
        catch (Exception ex)
        {
            return Result<BookmarkCheckResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error checking bookmark status for article '{ArticleId}'", articleId));
        }
    }

    public async Task<Result<PagedResult<BookmarkedArticleResModel>>> GetMyBookmarksAsync(BookmarkListReqModel req, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<PagedResult<BookmarkedArticleResModel>>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required to view your bookmarks.");
            }

            var query = _db.TblReaderBookmarks
                .AsNoTracking()
                .Include(b => b.Article)
                    .ThenInclude(a => a.ArticleCategory)
                .Include(b => b.Article)
                    .ThenInclude(a => a.Author)
                .Include(b => b.Article)
                    .ThenInclude(a => a.TblArticlePhotos)
                .Where(b => b.ReaderId == readerId && b.Article.Status == "Published");

            if (!string.IsNullOrWhiteSpace(req.ArticleCategoryId))
            {
                query = query.Where(b => b.Article.ArticleCategoryId == req.ArticleCategoryId);
            }

            if (!string.IsNullOrWhiteSpace(req.SearchKeyword))
            {
                var keyword = req.SearchKeyword.Trim();
                query = query.Where(b =>
                    b.Article.Title.Contains(keyword) ||
                    (b.Article.SubTitle != null && b.Article.SubTitle.Contains(keyword)));
            }

            var totalCount = await query.CountAsync(ct);

            var page = Math.Max(1, req.Page);
            var pageSize = Math.Clamp(req.PageSize, 1, 100);

            var bookmarks = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var articleIds = bookmarks.Select(b => b.ArticleId).ToList();

            var counts = await _db.TblReaderBookmarks
                .Where(b => articleIds.Contains(b.ArticleId))
                .GroupBy(b => b.ArticleId)
                .Select(g => new { ArticleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Count, ct);

            var items = bookmarks.Select(b => new BookmarkedArticleResModel
            {
                ReaderBookmarkId = b.ReaderBookmarkId,
                ArticleId = b.ArticleId,
                BookmarkedAt = b.CreatedAt,
                Article = new ArticleResModel
                {
                    ArticleId = b.Article.ArticleId,
                    Title = b.Article.Title,
                    SubTitle = b.Article.SubTitle,
                    Slug = b.Article.Slug,
                    Body = b.Article.Body,
                    PhotoUrl = b.Article.PhotoUrl,
                    PhotoCaption = b.Article.PhotoCaption,
                    PhotoCredit = b.Article.PhotoCredit,
                    ArticleCategoryId = b.Article.ArticleCategoryId,
                    CategoryName = b.Article.ArticleCategory?.Name ?? string.Empty,
                    AuthorId = b.Article.AuthorId,
                    AuthorName = $"{b.Article.Author?.FirstName} {b.Article.Author?.LastName}".Trim(),
                    Status = b.Article.Status,
                    PublishedAt = b.Article.PublishedAt,
                    IsFeatured = b.Article.IsFeatured,
                    IsSpotlight = b.Article.IsSpotlight,
                    CreatedAt = b.Article.CreatedAt,
                    UpdatedAt = b.Article.UpdatedAt,
                    IsBookmarked = true,
                    BookmarkCount = counts.GetValueOrDefault(b.ArticleId, 1),
                    Photos = b.Article.TblArticlePhotos
                        .OrderBy(p => p.SortOrder)
                        .Select(p => new ArticlePhotoResModel
                        {
                            ArticlePhotoId = p.ArticlePhotoId,
                            ArticleId = p.ArticleId,
                            RequestArticleId = p.RequestArticleId,
                            PhotoUrl = p.PhotoUrl,
                            Caption = p.Caption,
                            Credit = p.Credit,
                            SortOrder = p.SortOrder,
                            CreatedAt = p.CreatedAt
                        })
                        .ToList()
                }
            }).ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Result<PagedResult<BookmarkedArticleResModel>>.Success(new PagedResult<BookmarkedArticleResModel>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            return Result<PagedResult<BookmarkedArticleResModel>>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error retrieving bookmarked articles for reader"));
        }
    }
}
