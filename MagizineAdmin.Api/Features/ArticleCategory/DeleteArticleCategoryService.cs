using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Soft-deletes an article category by setting IsDeleted=true and stamping DeletedByAdminId.
/// </summary>
public sealed class DeleteArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly CurrentAdminAccessor _currentAdmin;

    public DeleteArticleCategoryService(MagizineDbContext db, CurrentAdminAccessor currentAdmin)
    {
        _db = db;
        _currentAdmin = currentAdmin;
    }

    public async Task ExecuteAsync(
        long id,
        CancellationToken ct = default)
    {
        var category = await _db.TblArticleCategories
            .FirstOrDefaultAsync(c => c.ArticleCategoryId == id, ct);

        if (category is null)
        {
            throw new ValidationException("ArticleCategoryId", "Category not found.");
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.DeletedByAdminId = _currentAdmin.AdminId;

        await _db.SaveChangesAsync(ct);
    }
}