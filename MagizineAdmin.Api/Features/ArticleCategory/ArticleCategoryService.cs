using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAdmin.Api.Features.ArticleCategory;

public class ArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly ILogger<ArticleCategoryService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ArticleCategoryService(
        MagizineDbContext db,
        ILogger<ArticleCategoryService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private long AuthorizedAdminId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value;
            return long.TryParse(sub, out var id) ? id : 0;
        }
    }

    public async Task<Result<PagedResult<ArticleCategoryResModel>>> GetArticleCategoryList(PageRequest reqModel, CancellationToken ct = default)
    {
        try
        {
            var query = _db.TblArticleCategories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.ArticleCategoryId);

            var paged = await query.ToPagedResultAsync(c => new ArticleCategoryResModel
            {
                ArticleCategoryId = c.ArticleCategoryId,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                SortOrder = c.SortOrder,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }, reqModel, ct);

            return Result<PagedResult<ArticleCategoryResModel>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<ArticleCategoryResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error"));
        }
    }

    public async Task<Result<ArticleCategoryResModel>> GetArticleCategoryById(ArticleCategoryDetailReqModel reqModel, CancellationToken ct = default)
    {
        try
        {
            var category = await _db.TblArticleCategories
                .Where(c => c.ArticleCategoryId == reqModel.ArticleCategoryId)
                .Select(c => new ArticleCategoryResModel
                {
                    ArticleCategoryId = c.ArticleCategoryId,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (category == null)
            {
                return Result<ArticleCategoryResModel>.Error("Category not found.");
            }

            return Result<ArticleCategoryResModel>.Success(category);
        }
        catch (Exception ex)
        {
            return Result<ArticleCategoryResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error"));
        }
    }

    public async Task<Result<ArticleCategoryResModel>> CreateArticleCategory(CreateArticleCategoryReqModel req, CancellationToken ct = default)
    {
        try
        {
            var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Name) : req.Slug;
            if (slug.Length > 30) slug = slug[..30].TrimEnd('-');

            var category = new TblArticleCategory
            {
                Name = req.Name,
                Slug = slug,
                Description = req.Description,
                SortOrder = req.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            _db.TblArticleCategories.Add(category);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return Result<ArticleCategoryResModel>.Error("A category with this slug already exists.");
            }

            var result = new ArticleCategoryResModel
            {
                ArticleCategoryId = category.ArticleCategoryId,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SortOrder = category.SortOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return Result<ArticleCategoryResModel>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ArticleCategoryResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error"));
        }
    }

    public async Task<Result<ArticleCategoryResModel>> UpdateArticleCategory(UpdateArticleCategoryReqModel req, CancellationToken ct = default)
    {
        try
        {
            var category = await _db.TblArticleCategories
                .FirstOrDefaultAsync(c => c.ArticleCategoryId == req.ArticleCategoryId, ct);

            if (category == null)
            {
                return Result<ArticleCategoryResModel>.Error("Category not found.");
            }

            var slug = string.IsNullOrWhiteSpace(req.Slug) ? Slugify(req.Name) : req.Slug;
            if (slug.Length > 30) slug = slug[..30].TrimEnd('-');

            category.Name = req.Name;
            category.Slug = slug;
            category.Description = req.Description;
            category.SortOrder = req.SortOrder;
            category.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return Result<ArticleCategoryResModel>.Error("A category with this slug already exists.");
            }

            var result = new ArticleCategoryResModel
            {
                ArticleCategoryId = category.ArticleCategoryId,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SortOrder = category.SortOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return Result<ArticleCategoryResModel>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ArticleCategoryResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error"));
        }
    }

    public async Task<Result<string>> DeleteArticleCategory(ArticleCategoryDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var category = await _db.TblArticleCategories
                .FirstOrDefaultAsync(c => c.ArticleCategoryId == req.ArticleCategoryId, ct);

            if (category == null)
            {
                return Result<string>.Error("Category not found.");
            }

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            
            var adminId = AuthorizedAdminId;
            if (adminId > 0)
            {
                category.DeletedByAdminId = adminId;
            }

            await _db.SaveChangesAsync(ct);
            
            return Result<string>.Success("Category deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error"));
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

        return result.Length == 0 ? "category" : result.ToString();
    }
}
