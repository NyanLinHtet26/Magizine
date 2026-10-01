using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Microsoft.AspNetCore.Mvc;

namespace MagizinePublic.Api.Features.Article;

[ApiController]
[Route("api/public/article")]
public sealed class ArticleController : ControllerBase
{
    private readonly ArticleService _articleService;

    public ArticleController(ArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpGet("list")]
    public async Task<ActionResult<Result<PagedResult<ArticleResModel>>>> GetList([FromQuery] ArticleListReqModel reqModel, CancellationToken ct)
    {
        var result = await _articleService.GetPublishedArticlesAsync(reqModel, ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<Result<ArticleResModel>>> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        var result = await _articleService.GetPublishedArticleBySlugAsync(slug, ct);
        return Ok(result);
    }
}
