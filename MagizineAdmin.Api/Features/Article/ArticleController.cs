using Magizine.Shared.Models.Article;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAdmin.Api.Features.Article;

// [Authorize] // Temporarily commented out for testing without a login token
[ApiController]
[Route("api/admin/article")]
public sealed class ArticleController : ControllerBase
{
    private readonly ArticleService _articleService;

    public ArticleController(ArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpPost("Detail")]
    public async Task<IActionResult> GetById([FromBody] ArticleDetailReqModel reqModel)
    {
        var result = await _articleService.GetArticleById(reqModel);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetList([FromBody] ArticleListReqModel reqModel)
    {
        var result = await _articleService.GetArticleList(reqModel);
        return Ok(result);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateArticleReqModel reqModel)
    {
        var result = await _articleService.CreateArticle(reqModel);
        return Ok(result);
    }

    [HttpPost("Update")]
    public async Task<IActionResult> Update([FromBody] UpdateArticleReqModel reqModel)
    {
        var result = await _articleService.UpdateArticle(reqModel);
        return Ok(result);
    }

    [HttpPost("ChangeStatus")]
    public async Task<IActionResult> ChangeStatus([FromBody] ChangeArticleStatusReqModel reqModel)
    {
        var result = await _articleService.ChangeArticleStatus(reqModel);
        return Ok(result);
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> Delete([FromBody] ArticleDetailReqModel reqModel)
    {
        var result = await _articleService.DeleteArticle(reqModel);
        return Ok(result);
    }
}
