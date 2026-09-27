using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Creates a new article category. Auto-generates slug from name if not provided.
/// Catches Postgres 23505 (unique_violation) and returns a business error.
/// </summary>
public sealed class CreateArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly CurrentAdminAccessor _currentAdmin;

    public CreateArticleCategoryService(MagizineDbContext db, CurrentAdminAccessor currentAdmin)
    {
        _db = db;
        _currentAdmin = currentAdmin;
    }

    public async Task<ArticleCategoryResModel> ExecuteAsync(
        CreateArticleCategoryReqModel req,
        CancellationToken ct = default)
    {
        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? Slugify(req.Name)
            : req.Slug!;

        // Ensure slug fits the column
        if (slug.Length > 30)
        {
            slug = slug[..30].TrimEnd('-');
        }

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
            // The partial unique index on (Name/Slug WHERE IsDeleted=false) caught a conflict
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
        // Simple slugification: lowercase, replace non-alphanumeric with hyphen, trim hyphens
        var result = new System.Text.StringBuilder(input.Length);
        bool prevWasHyphen = true; // start as true so we don't start with hyphen

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

        // Trim trailing hyphens
        while (result.Length > 0 && result[^1] == '-')
        {
            result.Remove(result.Length - 1, 1);
        }

        return result.Length == 0 ? "category" : result.ToString();
    }
}