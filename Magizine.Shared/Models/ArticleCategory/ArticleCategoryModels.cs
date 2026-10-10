using System.ComponentModel.DataAnnotations;

namespace Magizine.Shared.Models.ArticleCategory;

public sealed class CreateArticleCategoryReqModel
{
    [Required] [MaxLength(20)] public string Name { get; init; } = string.Empty;
    [MaxLength(30)] public string? Slug { get; init; }
    [MaxLength(200)] public string? Description { get; init; }
    public int SortOrder { get; init; } = 0;
}

public sealed class UpdateArticleCategoryReqModel
{
    [Required] public string ArticleCategoryId { get; init; } = string.Empty;
    [Required] [MaxLength(20)] public string Name { get; init; } = string.Empty;
    [MaxLength(30)] public string? Slug { get; init; }
    [MaxLength(200)] public string? Description { get; init; }
    public int SortOrder { get; init; } = 0;
}

public sealed class ArticleCategoryDetailReqModel
{
    [Required] public string ArticleCategoryId { get; init; } = string.Empty;
}

public sealed class ArticleCategoryResModel
{
    public string ArticleCategoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
