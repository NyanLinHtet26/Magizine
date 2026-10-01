using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAuthor.Api.Features.Article;

public sealed class ArticleService
{
    private readonly MagizineDbContext _db;
    private readonly ILogger<ArticleService> _logger;

    // TODO: Extract from HttpContext when Auth is fully active
    private long AuthorizedAuthorId => 2; // Temporarily hardcoded for Author testing

    public ArticleService(MagizineDbContext db, ILogger<ArticleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ArticleResModel>>> GetMyArticlesAsync(ArticleListReqModel req, CancellationToken ct = default)
    {
        try
        {
            // STRICT FILTER: Only show articles belonging to the logged-in author
            var query = _db.TblArticles
                .Include(a => a.ArticleCategory)
                .Include(a => a.Author)
                .Where(a => !a.IsDeleted && a.AuthorId == AuthorizedAuthorId)
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

            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                query = query.Where(a => a.Status.ToLower() == req.Status.ToLower());
            }

            // Order by most recently updated/created
            query = query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt);

            var pageReq = PageRequest.Create(req.Page, req.PageSize);

            var result = await query.ToPagedResultAsync(
                a => new ArticleResModel
                {
                    ArticleId = a.ArticleId,
                    Title = a.Title,
                    SubTitle = a.SubTitle,
                    Slug = a.Slug,
                    Body = null, // Don't return body in lists for bandwidth
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
            return Result<PagedResult<ArticleResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving author's article list"));
        }
    }

    public async Task<Result<ArticleResModel>> GetMyArticleByIdAsync(ArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var article = await _db.TblArticles
                .Include(a => a.ArticleCategory)
                .Include(a => a.Author)
                .Where(a => !a.IsDeleted && a.AuthorId == AuthorizedAuthorId && a.ArticleId == req.ArticleId)
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
                return Result<ArticleResModel>.Error("Article not found or you do not have permission to view it.");
            }

            return Result<ArticleResModel>.Success(article);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving author's article"));
        }
    }

    public async Task<Result<ArticleResModel>> CreateDraftAsync(CreateArticleReqModel req, CancellationToken ct = default)
    {
        try
        {
            var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
            if (!isCategoryValid)
                return Result<ArticleResModel>.Error("Selected Category does not exist.");

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
                
                // SECURITY: Ignore whatever AuthorId the frontend sent. Force it to the logged-in user.
                AuthorId = AuthorizedAuthorId,
                
                // SECURITY: Ignore whatever Status the frontend sent. Force it to Draft.
                Status = "Draft",
                IsFeatured = false,
                IsSpotlight = false,
                
                CreatedAt = DateTime.UtcNow
            };

            _db.TblArticles.Add(article);
            await _db.SaveChangesAsync(ct);

            return await GetMyArticleByIdAsync(new ArticleDetailReqModel { ArticleId = article.ArticleId }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result<ArticleResModel>.Error("An article with this slug already exists.");
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error creating draft"));
        }
    }

    public async Task<Result<ArticleResModel>> UpdateDraftAsync(UpdateArticleReqModel req, CancellationToken ct = default)
    {
        try
        {
            var article = await _db.TblArticles
                .FirstOrDefaultAsync(a => a.ArticleId == req.ArticleId && a.AuthorId == AuthorizedAuthorId && !a.IsDeleted, ct);

            if (article == null)
            {
                return Result<ArticleResModel>.Error("Article not found or you do not have permission to edit it.");
            }

            if (article.ArticleCategoryId != req.ArticleCategoryId)
            {
                var isCategoryValid = await _db.TblArticleCategories.AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId && !c.IsDeleted, ct);
                if (!isCategoryValid) return Result<ArticleResModel>.Error("Selected Category does not exist.");
            }

            article.Title = req.Title.Trim();
            article.SubTitle = req.SubTitle?.Trim();
            article.Body = req.Body;
            article.PhotoUrl = req.PhotoUrl;
            article.PhotoCaption = req.PhotoCaption;
            article.PhotoCredit = req.PhotoCredit;
            article.ArticleCategoryId = req.ArticleCategoryId;
            article.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return await GetMyArticleByIdAsync(new ArticleDetailReqModel { ArticleId = article.ArticleId }, ct);
        }
        catch (Exception ex)
        {
            return Result<ArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating draft"));
        }
    }

    public async Task<Result<string>> SubmitForReviewAsync(ArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var article = await _db.TblArticles
                .FirstOrDefaultAsync(a => a.ArticleId == req.ArticleId && a.AuthorId == AuthorizedAuthorId && !a.IsDeleted, ct);

            if (article == null)
            {
                return Result<string>.Error("Article not found or you do not have permission.");
            }

            if (article.Status != "Draft" && article.Status != "Rejected")
            {
                return Result<string>.Error($"Article is currently {article.Status} and cannot be submitted.");
            }

            // Change status to Pending so Admin can see it
            article.Status = "Pending";
            article.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Article successfully submitted for review.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error submitting article for review"));
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
