using System.ComponentModel.DataAnnotations;
using Magizine.Shared.Models.Paging;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Request model for creating an article category.
/// </summary>
public sealed class CreateArticleCategoryReqModel
{
    /// <summary>Display name, max 20 chars. Unique among non-deleted categories.</summary>
    [Required]
    [MaxLength(20)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// URL-friendly slug, max 30 chars. Unique among non-deleted categories.
    /// If omitted, auto-generated from <see cref="Name"/>.
    /// </summary>
    [MaxLength(30)]
    public string? Slug { get; init; }

    /// <summary>Optional description, max 200 chars.</summary>
    [MaxLength(200)]
    public string? Description { get; init; }

    /// <summary>Display order. Lower values appear first.</summary>
    public int SortOrder { get; init; } = 0;
}

/// <summary>
/// Request model for updating an article category.
/// </summary>
public sealed class UpdateArticleCategoryReqModel
{
    /// <summary>Display name, max 20 chars. Unique among non-deleted categories.</summary>
    [Required]
    [MaxLength(20)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// URL-friendly slug, max 30 chars. Unique among non-deleted categories.
    /// If omitted, auto-generated from <see cref="Name"/>.
    /// </summary>
    [MaxLength(30)]
    public string? Slug { get; init; }

    /// <summary>Optional description, max 200 chars.</summary>
    [MaxLength(200)]
    public string? Description { get; init; }

    /// <summary>Display order. Lower values appear first.</summary>
    public int SortOrder { get; init; } = 0;
}

/// <summary>
/// Response model for a single article category.
/// </summary>
public sealed class ArticleCategoryResModel
{
    public long ArticleCategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}