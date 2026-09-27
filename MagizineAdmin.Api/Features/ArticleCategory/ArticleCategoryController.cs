using Magizine.Shared.Models.ArticleCategory;
using Magizine.Shared.Models.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAdmin.Api.Features.ArticleCategory;

// [Authorize] // Temporarily commented out for testing without a login token
[ApiController]
[Route("api/admin/article-category")]
public sealed class ArticleCategoryController : ControllerBase
{
    private readonly ArticleCategoryService _articleCategoryService;

    public ArticleCategoryController(ArticleCategoryService articleCategoryService)
    {
        _articleCategoryService = articleCategoryService;
    }

    [HttpPost("Detail")]
    public async Task<IActionResult> GetById([FromBody] ArticleCategoryDetailReqModel reqModel)
    {
        var result = await _articleCategoryService.GetArticleCategoryById(reqModel);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetList([FromBody] PageRequest reqModel)
    {
        var result = await _articleCategoryService.GetArticleCategoryList(reqModel);
        return Ok(result);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateArticleCategoryReqModel reqModel)
    {
        var result = await _articleCategoryService.CreateArticleCategory(reqModel);
        return Ok(result);
    }

    [HttpPost("Update")]
    public async Task<IActionResult> Update([FromBody] UpdateArticleCategoryReqModel reqModel)
    {
        var result = await _articleCategoryService.UpdateArticleCategory(reqModel);
        return Ok(result);
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> Delete([FromBody] ArticleCategoryDetailReqModel reqModel)
    {
        var result = await _articleCategoryService.DeleteArticleCategory(reqModel);
        return Ok(result);
    }
}