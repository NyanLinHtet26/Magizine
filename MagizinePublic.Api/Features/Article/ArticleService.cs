using Magizine.DataBase;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizinePublic.Api.Features.Article;

public sealed class ArticleService
{
    private readonly MagizineDbContext _db;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(MagizineDbContext db, ILogger<ArticleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ArticleResModel>>> GetPublishedArticlesAsync(ArticleListReqModel req, CancellationToken ct = default)
    {
        try
        {
            // PUBLIC API RULE: Only ever return Published & Non-Deleted articles.
            var query = _db.TblArticles
                .Include(a => a.ArticleCategory)
                .Include(a => a.Author)
                .Where(a => !a.IsDeleted && a.Status == "Published")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.SearchKeyword))
            {
                var keyword = req.SearchKeyword.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(keyword) 
                                      || (a.SubTitle != null && a.SubTitle.ToLower().Contains(keyword)));
            }

            if (req.ArticleCategoryId.HasValue && req.ArticleCategoryId.Value > 0)
            {
                query = query.Where(a => a.ArticleCategoryId == req.ArticleCategoryId.Value);
            }

            if (req.AuthorId.HasValue && req.AuthorId.Value > 0)
            {
                query = query.Where(a => a.AuthorId == req.AuthorId.Value);
            }

            if (req.IsFeatured.HasValue)
            {
                query = query.Where(a => a.IsFeatured == req.IsFeatured.Value);
            }

            if (req.IsSpotlight.HasValue)
            {
                query = query.Where(a => a.IsSpotlight == req.IsSpotlight.Value);
            }

            // Public sorting: always most recently published first
            query = query.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt);

            var pageReq = PageRequest.Create(req.Page, req.PageSize);

            var result = await query.ToPagedResultAsync(
                a => new ArticleResModel
                {
                    ArticleId = a.ArticleId,
                    Title = a.Title,
                    SubTitle = a.SubTitle,
                    Slug = a.Slug,
                    Body = a.Body, // Depending on payload size, you might want a separate model for list vs detail that excludes Body
                    PhotoUrl = a.PhotoUrl,
                    PhotoCaption = a.PhotoCaption,
                    PhotoCredit = a.PhotoCredit,
                    ArticleCategoryId = a.ArticleCategoryId,
                    CategoryName = a.ArticleCategory.Name,
                    AuthorId = a.AuthorId,
                    AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                    Status = a.Status,
                    PublishedAt = a.PublishedAt,
                    IsFeatured = a.IsFeatured,
                    IsSpotlight = a.IsSpotlight,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                },
                pageReq,
                ct);

            return Result<PagedResult<ArticleResModel>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ArticleResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving public article list"));
        }
    }

    public async Task<Result<ArticleResModel>> GetPublishedArticleBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var article = await _db.TblArticles
                .Include(a => a.ArticleCategory)
                .Include(a => a.Author)
                .Where(a => !a.IsDeleted && a.Status == "Published" && a.Slug == slug)
                .Select(a => new ArticleResModel
                {
                    ArticleId = a.ArticleId,
                    Title = a.Title,
                    SubTitle = a.SubTitle,
                    Slug = a.Slug,
                    Body = a.Body,
                    PhotoUrl = a.PhotoUrl,
                    PhotoCaption = a.PhotoCaption,
                    PhotoCredit = a.PhotoCredit,
                    ArticleCategoryId = a.ArticleCategoryId,
                    CategoryName = a.ArticleCategory.Name,
                    AuthorId = a.AuthorId,
                    AuthorName = a.Author.FirstName + " " + a.Author.LastName,
                    Status = a.Status,
                    PublishedAt = a.PublishedAt,
                    IsFeatured = a.IsFeatured,
                    IsSpotlight = a.IsSpotlight,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (article == null)
            {
                return Result<ArticleResModel>.Error("Article not found.");
            }

            return Result<ArticleResModel>.Success(article);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving public article by slug"));
        }
    }
}
