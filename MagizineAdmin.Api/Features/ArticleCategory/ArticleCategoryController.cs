using MagizineAdmin.Api.Features.ArticleCategory;
using Magizine.Shared.Models.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Admin CRUD for article categories. Requires authentication (any valid admin token).
/// </summary>
[Authorize]
[ApiController]
[Route("api/admin/article-category")]
public sealed class ArticleCategoryController : ControllerBase
{
    private readonly ListArticleCategoryService _listService;
    private readonly GetArticleCategoryByIdService _getService;
    private readonly CreateArticleCategoryService _createService;
    private readonly UpdateArticleCategoryService _updateService;
    private readonly DeleteArticleCategoryService _deleteService;

    public ArticleCategoryController(
        ListArticleCategoryService listService,
        GetArticleCategoryByIdService getService,
        CreateArticleCategoryService createService,
        UpdateArticleCategoryService updateService,
        DeleteArticleCategoryService deleteService)
    {
        _listService = listService;
        _getService = getService;
        _createService = createService;
        _updateService = updateService;
        _deleteService = deleteService;
    }

    /// <summary>
    /// Returns a paged list of categories ordered by SortOrder then Id.
    /// </summary>
    /// <param name="page">1-based page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleCategoryResModel>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = PageRequest.Create(page, pageSize);
        var result = await _listService.ExecuteAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single category by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ArticleCategoryResModel>> GetById(
        long id,
        CancellationToken ct = default)
    {
        var category = await _getService.ExecuteAsync(id, ct);
        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>
    /// Creates a new category. Slug is auto-generated from Name if omitted.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ArticleCategoryResModel>> Create(
        CreateArticleCategoryReqModel req,
        CancellationToken ct = default)
    {
        var category = await _createService.ExecuteAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = category.ArticleCategoryId }, category);
    }

    /// <summary>
    /// Updates an existing category. Slug is auto-generated from Name if omitted.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ArticleCategoryResModel>> Update(
        long id,
        UpdateArticleCategoryReqModel req,
        CancellationToken ct = default)
    {
        var category = await _updateService.ExecuteAsync(id, req, ct);
        return Ok(category);
    }

    /// <summary>
    /// Soft-deletes a category. The name/slug become available for reuse.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(
        long id,
        CancellationToken ct = default)
    {
        await _deleteService.ExecuteAsync(id, ct);
        return NoContent();
    }
}