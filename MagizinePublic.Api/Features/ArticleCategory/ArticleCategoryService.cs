using Magizine.DataBase;
using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Microsoft.EntityFrameworkCore;

namespace MagizinePublic.Api.Features.ArticleCategory;

public class ArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly ILogger<ArticleCategoryService> _logger;

    public ArticleCategoryService(
        MagizineDbContext db,
        ILogger<ArticleCategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<ArticleCategoryResModel>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var categories = await _db.TblArticleCategories
                .Where(c => c.IsDeleted == false)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.ArticleCategoryId)
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
                .ToListAsync(ct);

            return Result<List<ArticleCategoryResModel>>.Success(categories);
        }
        catch (Exception ex)
        {
            return Result<List<ArticleCategoryResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error fetching public categories"));
        }
    }

    public async Task<Result<ArticleCategoryResModel>> GetCategoryBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            var category = await _db.TblArticleCategories
                .Where(c => c.IsDeleted == false && c.Slug == slug)
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
            return Result<ArticleCategoryResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error fetching category by slug"));
        }
    }
}
