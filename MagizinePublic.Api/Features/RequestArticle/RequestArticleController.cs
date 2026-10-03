using Magizine.Shared.Models;
using Magizine.Shared.Models.RequestArticle;
using Microsoft.AspNetCore.Mvc;

namespace MagizinePublic.Api.Features.RequestArticle;

[ApiController]
[Route("api/public/requestarticle")]
public sealed class RequestArticleController : ControllerBase
{
    private readonly RequestArticleService _requestArticleService;

    public RequestArticleController(RequestArticleService requestArticleService)
    {
        _requestArticleService = requestArticleService;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<Result<string>>> SubmitPitch([FromBody] CreateRequestArticleReqModel reqModel, CancellationToken ct)
    {
        var result = await _requestArticleService.SubmitPitchAsync(reqModel, ct);
        return Ok(result);
    }
}
