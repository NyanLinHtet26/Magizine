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
    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly ILogger<ArticleService> _logger;

    // TODO: Extract from HttpContext when Auth is fully active
    private long AuthorizedAdminId => 1;

    public ArticleService(MagizineDbContext db, DapperService dapperService, ILogger<ArticleService> logger)
    {
        _db = db;
        _dapperService = dapperService;
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
                new { p_article_id = req.ArticleId, p_author_id = (long?)null });

            if (article == null)
            {
                return Result<ArticleResModel>.Error("Article not found.");
            }

            return Result<ArticleResModel>.Success(article);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving article via Dapper"));
        }
    }

    public async Task<Result<ArticleResModel>> CreateArticle(CreateArticleReqModel req, CancellationToken ct = default)
    {
        try
        {
            var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
            if (!isCategoryValid)
                return Result<ArticleResModel>.Error("Selected Category does not exist.");

            var isAuthorValid = await _db.TblAuthors.AnyAsync(a => a.AuthorId == req.AuthorId && !a.IsDeleted, ct);
            if (!isAuthorValid)
                return Result<ArticleResModel>.Error("Selected Author does not exist.");

            var slug = Slugify(req.Title);
            var isSlugTaken = await _db.TblArticles.AnyAsync(a => a.Slug == slug && !a.IsDeleted, ct);
            if (isSlugTaken)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            var article = new TblArticle
            {
                Title = req.Title.Trim(),
                SubTitle = req.SubTitle?.Trim(),
                Slug = slug,
                Body = req.Body,
                PhotoUrl = req.PhotoUrl,
                PhotoCaption = req.PhotoCaption,
                PhotoCredit = req.PhotoCredit,
                ArticleCategoryId = req.ArticleCategoryId,
                AuthorId = req.AuthorId,
                Status = req.Status ?? "Draft",
                IsFeatured = req.IsFeatured,
                IsSpotlight = req.IsSpotlight,
                CreatedAt = DateTime.UtcNow,
                PublishedAt = req.Status == "Published" ? DateTime.UtcNow : null
            };

            _db.TblArticles.Add(article);
            await _db.SaveChangesAsync(ct);

            return await GetArticleById(new ArticleDetailReqModel { ArticleId = article.ArticleId }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result<ArticleResModel>.Error("An article with this slug already exists.");
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error creating article"));
        }
    }

    public async Task<Result<ArticleResModel>> UpdateArticle(UpdateArticleReqModel req, CancellationToken ct = default)
    {
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

            // Only update slug if Title has drastically changed? Typically, we keep original slug to not break SEO.
            // We will leave the slug alone on update unless explicitly asked.
            
            article.Title = req.Title.Trim();
            article.SubTitle = req.SubTitle?.Trim();
            article.Body = req.Body;
            article.PhotoUrl = req.PhotoUrl;
            article.PhotoCaption = req.PhotoCaption;
            article.PhotoCredit = req.PhotoCredit;
            article.ArticleCategoryId = req.ArticleCategoryId;
            article.AuthorId = req.AuthorId;
            article.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return await GetArticleById(new ArticleDetailReqModel { ArticleId = article.ArticleId }, ct);
        }
        catch (Exception ex)
        {
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
                article.PublishedAt = DateTime.UtcNow;
            }
            else if (req.Status != "Published")
            {
                article.PublishedAt = null; // Or keep it if you want to track original publish date
            }

            article.Status = req.Status;
            article.IsFeatured = req.IsFeatured;
            article.IsSpotlight = req.IsSpotlight;
            article.UpdatedAt = DateTime.UtcNow;

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

            article.IsDeleted = true;
            article.DeletedAt = DateTime.UtcNow;
            
            var adminId = AuthorizedAdminId;
            if (adminId > 0)
            {
                article.DeletedByAdminId = adminId;
            }

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Article deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error deleting article"));
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";

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
