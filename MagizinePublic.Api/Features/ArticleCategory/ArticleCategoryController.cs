using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Microsoft.AspNetCore.Mvc;

namespace MagizinePublic.Api.Features.ArticleCategory;

[ApiController]
[Route("api/public/article-category")]
public sealed class ArticleCategoryController : ControllerBase
{
    private readonly ArticleCategoryService _articleCategoryService;

    public ArticleCategoryController(ArticleCategoryService articleCategoryService)
    {
        _articleCategoryService = articleCategoryService;
    }

    [HttpGet("list")]
    public async Task<ActionResult<Result<List<ArticleCategoryResModel>>>> GetList(CancellationToken ct)
    {
        var result = await _articleCategoryService.GetCategoriesAsync(ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Result<ArticleCategoryResModel>>> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        var result = await _articleCategoryService.GetCategoryBySlugAsync(slug, ct);
        return Ok(result);
    }
}
