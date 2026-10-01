using Magizine.Shared.Models.Article;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAuthor.Api.Features.Article;

// [Authorize] // Temporarily commented out for testing
[ApiController]
[Route("api/author/article")]
public sealed class ArticleController : ControllerBase
{
    private readonly ArticleService _articleService;

    public ArticleController(ArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpPost("Detail")]
    public async Task<IActionResult> GetMyArticleById([FromBody] ArticleDetailReqModel reqModel)
    {
        var result = await _articleService.GetMyArticleByIdAsync(reqModel);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetMyArticles([FromBody] ArticleListReqModel reqModel)
    {
        var result = await _articleService.GetMyArticlesAsync(reqModel);
        return Ok(result);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateDraft([FromBody] CreateArticleReqModel reqModel)
    {
        var result = await _articleService.CreateDraftAsync(reqModel);
        return Ok(result);
    }

    [HttpPost("Update")]
    public async Task<IActionResult> UpdateDraft([FromBody] UpdateArticleReqModel reqModel)
    {
        var result = await _articleService.UpdateDraftAsync(reqModel);
        return Ok(result);
    }

    [HttpPost("SubmitForReview")]
    public async Task<IActionResult> SubmitForReview([FromBody] ArticleDetailReqModel reqModel)
    {
        var result = await _articleService.SubmitForReviewAsync(reqModel);
        return Ok(result);
    }
}
