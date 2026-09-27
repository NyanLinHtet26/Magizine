using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Updates an existing article category. Auto-generates slug from name if not provided.
/// Catches Postgres 23505 (unique_violation) and returns a business error.
/// </summary>
public sealed class UpdateArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly CurrentAdminAccessor _currentAdmin;

    public UpdateArticleCategoryService(MagizineDbContext db, CurrentAdminAccessor currentAdmin)
    {
        _db = db;
        _currentAdmin = currentAdmin;
    }

    public async Task<ArticleCategoryResModel> ExecuteAsync(
        long id,
        UpdateArticleCategoryReqModel req,
        CancellationToken ct = default)
    {
        var category = await _db.TblArticleCategories
            .FirstOrDefaultAsync(c => c.ArticleCategoryId == id, ct);

        if (category is null)
        {
            throw new ValidationException("ArticleCategoryId", "Category not found.");
        }

        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? Slugify(req.Name)
            : req.Slug!;

        if (slug.Length > 30)
        {
            slug = slug[..30].TrimEnd('-');
        }

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
            throw new ValidationException("Slug", "A category with this slug already exists.");
        }

        return new ArticleCategoryResModel
        {
            ArticleCategoryId = category.ArticleCategoryId,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
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