using Magizine.Shared.Models;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Models.Reader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizinePublic.Api.Features.Bookmark;

[ApiController]
[Route("api/public/bookmark")]
public sealed class BookmarkController : ControllerBase
{
    private readonly BookmarkService _bookmarkService;

    public BookmarkController(BookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [Authorize]
    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle([FromBody] BookmarkArticleReqModel req, CancellationToken ct)
    {
        var result = await _bookmarkService.ToggleBookmarkAsync(req.ArticleId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("toggle/{articleId}")]
    public async Task<IActionResult> ToggleByRoute([FromRoute] string articleId, CancellationToken ct)
    {
        var result = await _bookmarkService.ToggleBookmarkAsync(articleId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] BookmarkArticleReqModel req, CancellationToken ct)
    {
        var result = await _bookmarkService.AddBookmarkAsync(req.ArticleId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("add/{articleId}")]
    public async Task<IActionResult> AddByRoute([FromRoute] string articleId, CancellationToken ct)
    {
        var result = await _bookmarkService.AddBookmarkAsync(articleId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("remove")]
    [HttpDelete("remove")]
    public async Task<IActionResult> Remove([FromBody] BookmarkArticleReqModel req, CancellationToken ct)
    {
        var result = await _bookmarkService.RemoveBookmarkAsync(req.ArticleId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{articleId}")]
    [HttpPost("remove/{articleId}")]
    public async Task<IActionResult> RemoveByRoute([FromRoute] string articleId, CancellationToken ct)
    {
        var result = await _bookmarkService.RemoveBookmarkAsync(articleId, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("check/{articleId}")]
    public async Task<IActionResult> CheckStatus([FromRoute] string articleId, CancellationToken ct)
    {
        var result = await _bookmarkService.IsArticleBookmarkedAsync(articleId, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("my-bookmarks")]
    [HttpGet("list")]
    public async Task<ActionResult<Result<PagedResult<BookmarkedArticleResModel>>>> GetMyBookmarks(
        [FromQuery] BookmarkListReqModel req,
        CancellationToken ct)
    {
        var result = await _bookmarkService.GetMyBookmarksAsync(req, ct);
        return Ok(result);
    }
}
