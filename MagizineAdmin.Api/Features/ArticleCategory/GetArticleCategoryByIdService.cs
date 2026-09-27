using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Returns a single article category by ID.
/// </summary>
public sealed class GetArticleCategoryByIdService
{
    private readonly MagizineDbContext _db;

    public GetArticleCategoryByIdService(MagizineDbContext db)
    {
        _db = db;
    }

    public async Task<ArticleCategoryResModel?> ExecuteAsync(
        long id,
        CancellationToken ct = default)
    {
        var category = await _db.TblArticleCategories
            .Where(c => c.ArticleCategoryId == id)
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

        return category;
    }
}