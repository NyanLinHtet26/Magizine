using Magizine.DataBase;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Security;
using Magizine.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizinePublic.Api.Features.Article;

public sealed class ArticleService
{
    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(
        MagizineDbContext db,
        DapperService dapperService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ArticleService> logger)
    {
        _db = db;
        _dapperService = dapperService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string? CallerReaderId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value;

    public async Task<Result<PagedResult<ArticleResModel>>> GetPublishedArticlesAsync(ArticleListReqModel req, CancellationToken ct = default)
    {
        try
        {
            var pageReq = PageRequest.Create(req.Page, req.PageSize);
            
            var parameters = new
            {
                p_search_keyword = req.SearchKeyword,
                p_category_id = req.ArticleCategoryId,
                p_author_id = req.AuthorId,
                p_status = "Published", // PUBLIC API RULE: STRICTLY Published
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

            if (paged.Items.Count > 0)
            {
                var articleIds = paged.Items.Select(x => x.ArticleId).ToList();

                var counts = await _db.TblReaderBookmarks
                    .AsNoTracking()
                    .Where(b => articleIds.Contains(b.ArticleId))
                    .GroupBy(b => b.ArticleId)
                    .Select(g => new { ArticleId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ArticleId, x => x.Count, ct);

                var readerId = CallerReaderId;
                HashSet<string>? userBookmarkedIds = null;

                if (!string.IsNullOrWhiteSpace(readerId))
                {
                    userBookmarkedIds = (await _db.TblReaderBookmarks
                        .AsNoTracking()
                        .Where(b => b.ReaderId == readerId && articleIds.Contains(b.ArticleId))
                        .Select(b => b.ArticleId)
                        .ToListAsync(ct)).ToHashSet();
                }

                foreach (var item in paged.Items)
                {
                    item.BookmarkCount = counts.GetValueOrDefault(item.ArticleId, 0);
                    item.IsBookmarked = userBookmarkedIds?.Contains(item.ArticleId) ?? false;
                }
            }

            return Result<PagedResult<ArticleResModel>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ArticleResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving public article list via Dapper"));
        }
    }

    public async Task<Result<ArticleResModel>> GetPublishedArticleBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var article = await _dapperService.GetFirstOrDefaultAsync<ArticleResModel>(
                "fn_get_article_by_slug", 
                new { p_slug = slug });

            // Ensure it's actually published (in case fn_get_article_by_slug doesn't filter status)
            if (article == null || article.Status != "Published")
            {
                return Result<ArticleResModel>.Error("Article not found.");
            }

            article.Photos = await _dapperService.GetListAsync<ArticlePhotoResModel>(
                "fn_get_article_photos_by_article_id",
                new { p_article_id = article.ArticleId });

            var count = await _db.TblReaderBookmarks
                .AsNoTracking()
                .CountAsync(b => b.ArticleId == article.ArticleId, ct);

            article.BookmarkCount = count;

            var readerId = CallerReaderId;
            if (!string.IsNullOrWhiteSpace(readerId))
            {
                article.IsBookmarked = await _db.TblReaderBookmarks
                    .AsNoTracking()
                    .AnyAsync(b => b.ReaderId == readerId && b.ArticleId == article.ArticleId, ct);
            }
            else
            {
                article.IsBookmarked = false;
            }

            return Result<ArticleResModel>.Success(article);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving public article by slug via Dapper"));
        }
    }
}
