using Magizine.Shared.Models.Article;
using Magizine.Shared.Services;
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
    public async Task<IActionResult> GetMyArticleById([FromBody] ArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetMyArticleByIdAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetMyArticles([FromBody] ArticleListReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetMyArticlesAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Create")]
    [RequestSizeLimit(FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> CreateDraft([FromForm] CreateArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.CreateDraftAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Update")]
    [RequestSizeLimit(FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> UpdateDraft([FromForm] UpdateArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.UpdateDraftAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("SubmitForReview")]
    public async Task<IActionResult> SubmitForReview([FromBody] ArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.SubmitForReviewAsync(reqModel, ct);
        return Ok(result);
    }
}
