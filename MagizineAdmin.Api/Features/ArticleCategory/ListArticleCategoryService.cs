using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.DataBase.Paging;
using Magizine.Shared.Exceptions;
using Magizine.Shared.Models.Paging;
using Microsoft.EntityFrameworkCore;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Returns a paged list of article categories ordered by SortOrder then Id.
/// </summary>
public sealed class ListArticleCategoryService
{
    private readonly MagizineDbContext _db;

    public ListArticleCategoryService(MagizineDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ArticleCategoryResModel>> ExecuteAsync(
        PageRequest request,
        CancellationToken ct = default)
    {
        var query = _db.TblArticleCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ArticleCategoryId);

        return await query.ToPagedResultAsync(c => new ArticleCategoryResModel
        {
            ArticleCategoryId = c.ArticleCategoryId,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            SortOrder = c.SortOrder,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }, request, ct);
    }
}