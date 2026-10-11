using Magizine.Shared.Models.RequestArticle;
using Magizine.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAuthor.Api.Features.Article;

[Authorize]
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
    public async Task<IActionResult> GetMyArticleById([FromBody] RequestArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetMyArticleByIdAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetMyArticles([FromBody] RequestArticleListReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetMyArticlesAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Create")]
    [RequestSizeLimit(FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> Create([FromForm] CreateRequestArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.CreateArticleAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Update")]
    [RequestSizeLimit(FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> Update([FromForm] UpdateRequestArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.UpdateArticleAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("SubmitForReview")]
    public async Task<IActionResult> SubmitForReview([FromBody] RequestArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.SubmitForReviewAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> Delete([FromBody] RequestArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.DeleteMyArticleAsync(reqModel, ct);
        return Ok(result);
    }
}
