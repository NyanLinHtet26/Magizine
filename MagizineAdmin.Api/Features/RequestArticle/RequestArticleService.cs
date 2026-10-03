using Magizine.DataBase;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Models.RequestArticle;
using Magizine.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAdmin.Api.Features.RequestArticle;

public sealed class RequestArticleService
{
    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly ILogger<RequestArticleService> _logger;

    // TODO: Extract from HttpContext when Auth is fully active
    private long AuthorizedAdminId => 1;

    public RequestArticleService(MagizineDbContext db, DapperService dapperService, ILogger<RequestArticleService> logger)
    {
        _db = db;
        _dapperService = dapperService;
        _logger = logger;
    }

    public async Task<Result<PagedResult<RequestArticleResModel>>> GetRequestArticleList(RequestArticleListReqModel req, CancellationToken ct = default)
    {
        try
        {
            var pageReq = PageRequest.Create(req.Page, req.PageSize);
            
            var parameters = new
            {
                p_search_keyword = req.SearchKeyword,
                p_status = req.Status,
                p_page = pageReq.Page,
                p_page_size = pageReq.PageSize
            };

            var paged = await _dapperService.GetPagedListAsync<RequestArticleResModel>(
                "fn_get_request_article_list", 
                parameters, 
                "RequestArticleId", 
                pageReq);

            return Result<PagedResult<RequestArticleResModel>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<RequestArticleResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving request article list via Dapper"));
        }
    }

    public async Task<Result<RequestArticleResModel>> GetRequestArticleById(RequestArticleDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var requestArticle = await _dapperService.GetFirstOrDefaultAsync<RequestArticleResModel>(
                "fn_get_request_article_by_id", 
                new { p_request_id = req.RequestArticleId });

            if (requestArticle == null)
            {
                return Result<RequestArticleResModel>.Error("Request Article not found.");
            }

            return Result<RequestArticleResModel>.Success(requestArticle);
        }
        catch (Exception ex)
        {
            return Result<RequestArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving request article via Dapper"));
        }
    }

    public async Task<Result<RequestArticleResModel>> ReviewRequestArticle(ReviewRequestArticleReqModel req, CancellationToken ct = default)
    {
        try
        {
            var request = await _db.TblRequestArticles
                .FirstOrDefaultAsync(r => r.RequestArticleId == req.RequestArticleId && !r.IsDeleted, ct);

            if (request == null)
            {
                return Result<RequestArticleResModel>.Error("Request Article not found.");
            }

            request.Status = req.Status;
            request.AdminNotes = req.AdminNotes;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByAdminId = AuthorizedAdminId;
            request.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // Fetch the updated model via Dapper to return
            return await GetRequestArticleById(new RequestArticleDetailReqModel { RequestArticleId = request.RequestArticleId }, ct);
        }
        catch (Exception ex)
        {
            return Result<RequestArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error reviewing request article"));
        }
    }
}
