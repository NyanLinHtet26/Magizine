using Magizine.DataBase;
using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAuthor.Api.Features.ArticleCategory;

public sealed class ArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly ILogger<ArticleCategoryService> _logger;

    public ArticleCategoryService(MagizineDbContext db, ILogger<ArticleCategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<ArticleCategoryResModel>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var categories = await _db.TblArticleCategories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.SortOrder)
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
            return Result<List<ArticleCategoryResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving categories for author"));
        }
    }
}
