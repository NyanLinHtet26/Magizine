using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAuthor.Api.Features.ArticleCategory;

// [Authorize] // Temporarily commented out for testing
[ApiController]
[Route("api/author/article-category")]
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
        // Provides the dropdown list of categories for the author when writing an article
        var result = await _articleCategoryService.GetCategoriesAsync(ct);
        return Ok(result);
    }
}
