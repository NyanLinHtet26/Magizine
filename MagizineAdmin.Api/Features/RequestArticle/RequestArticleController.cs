using Magizine.Shared.Models.RequestArticle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAdmin.Api.Features.RequestArticle;

// [Authorize] // Temporarily commented out for testing
[ApiController]
[Route("api/admin/requestarticle")]
public sealed class RequestArticleController : ControllerBase
{
    private readonly RequestArticleService _requestArticleService;

    public RequestArticleController(RequestArticleService requestArticleService)
    {
        _requestArticleService = requestArticleService;
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetList([FromBody] RequestArticleListReqModel reqModel, CancellationToken ct)
    {
        var result = await _requestArticleService.GetRequestArticleList(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Detail")]
    public async Task<IActionResult> GetDetail([FromBody] RequestArticleDetailReqModel reqModel, CancellationToken ct)
    {
        var result = await _requestArticleService.GetRequestArticleById(reqModel, ct);
        return Ok(result);
    }

    [HttpPost("Review")]
    public async Task<IActionResult> ReviewRequest([FromBody] ReviewRequestArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _requestArticleService.ReviewRequestArticle(reqModel, ct);
        return Ok(result);
    }
}
