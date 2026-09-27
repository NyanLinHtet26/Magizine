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
    [Required] public long ArticleCategoryId { get; init; }
    [Required] [MaxLength(20)] public string Name { get; init; } = string.Empty;
    [MaxLength(30)] public string? Slug { get; init; }
    [MaxLength(200)] public string? Description { get; init; }
    public int SortOrder { get; init; } = 0;
}

public sealed class ArticleCategoryDetailReqModel
{
    [Required] public long ArticleCategoryId { get; init; }
}

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
