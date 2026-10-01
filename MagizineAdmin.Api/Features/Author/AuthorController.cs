using Magizine.Shared.Models.Author;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAdmin.Api.Features.Author;

// [Authorize] // Temporarily commented out for testing without a login token
[ApiController]
[Route("api/admin/author")]
public sealed class AuthorController : ControllerBase
{
    private readonly AuthorService _authorService;

    public AuthorController(AuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpPost("Detail")]
    public async Task<IActionResult> GetById([FromBody] AuthorDetailReqModel reqModel)
    {
        var result = await _authorService.GetAuthorById(reqModel);
        return Ok(result);
    }

    [HttpPost("List")]
    public async Task<IActionResult> GetList([FromBody] AuthorListReqModel reqModel)
    {
        var result = await _authorService.GetAuthorList(reqModel);
        return Ok(result);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateAuthorReqModel reqModel)
    {
        var result = await _authorService.CreateAuthor(reqModel);
        return Ok(result);
    }

    [HttpPost("Update")]
    public async Task<IActionResult> Update([FromBody] UpdateAuthorReqModel reqModel)
    {
        var result = await _authorService.UpdateAuthor(reqModel);
        return Ok(result);
    }

    [HttpPost("ChangeStatus")]
    public async Task<IActionResult> ChangeStatus([FromBody] ChangeAuthorStatusReqModel reqModel)
    {
        var result = await _authorService.ChangeAuthorStatus(reqModel);
        return Ok(result);
    }

    [HttpPost("Delete")]
    public async Task<IActionResult> Delete([FromBody] AuthorDetailReqModel reqModel)
    {
        var result = await _authorService.DeleteAuthor(reqModel);
        return Ok(result);
    }
}
