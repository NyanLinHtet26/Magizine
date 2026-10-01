using Magizine.DataBase;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizinePublic.Api.Features.Article;

public sealed class ArticleService
{
    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(MagizineDbContext db, DapperService dapperService, ILogger<ArticleService> logger)
    {
        _db = db;
        _dapperService = dapperService;
        _logger = logger;
    }

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

            return Result<ArticleResModel>.Success(article);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving public article by slug via Dapper"));
        }
    }
}
