using Magizine.Shared.Models.Article;
using Magizine.Shared.Services;
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
    public async Task<IActionResult> GetById([FromBody] ArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetArticleById(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetList([FromBody] ArticleListReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetArticleList(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Create")]
    [RequestSizeLimit(FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> Create([FromForm] CreateArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.CreateArticle(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Update")]
    [RequestSizeLimit(FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileStorageExtensions.MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> Update([FromForm] UpdateArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.UpdateArticle(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("ChangeStatus")]
    public async Task<IActionResult> ChangeStatus([FromBody] ChangeArticleStatusReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.ChangeArticleStatus(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> Delete([FromBody] ArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.DeleteArticle(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("PurgeExpiredPhotos")]
    public async Task<IActionResult> PurgeExpiredPhotos([FromBody] PurgeExpiredPhotosReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.PurgeExpiredPhotosAsync(reqModel, ct);
        return Ok(result);
    }
}
