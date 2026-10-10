using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Enums;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Article;
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
    private string AuthorizedAdminId => "01JADMIN000000000000000001";

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
            // Drafts are private to Authors and hidden from Admin
            if (string.Equals(req.Status?.Trim(), EnumRequestArticleStatus.Draft.ToDbString(), StringComparison.OrdinalIgnoreCase))
            {
                var emptyPageReq = PageRequest.Create(req.Page, req.PageSize);
                return Result<PagedResult<RequestArticleResModel>>.Success(
                    PagedResult<RequestArticleResModel>.Create([], 0, emptyPageReq));
            }

            var pageReq = PageRequest.Create(req.Page, req.PageSize);

            var parameters = new
            {
                p_search_keyword = req.SearchKeyword,
                p_category_id = req.ArticleCategoryId,
                p_author_id = req.AuthorId,
                p_status = req.Status,
                p_exclude_draft = true, // STRICT RULE: Hide Author 'Draft' rows from Admin
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
                new
                {
                    p_request_id = req.RequestArticleId,
                    p_author_id = (string?)null,
                    p_exclude_draft = true // Hide Draft from Admin
                });

            if (requestArticle == null)
            {
                return Result<RequestArticleResModel>.Error("Request Article not found.");
            }

            requestArticle.Photos = await _dapperService.GetListAsync<ArticlePhotoResModel>(
                "fn_get_article_photos_by_request_id",
                new { p_request_id = requestArticle.RequestArticleId });

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
            var normalizedDecision = NormalizeAdminDecision(req.Status);
            if (normalizedDecision is null)
            {
                return Result<RequestArticleResModel>.Error("Review Status must be either 'Approved' or 'Rejected'.");
            }

            var request = await _db.TblRequestArticles
                .FirstOrDefaultAsync(r => r.RequestArticleId == req.RequestArticleId && !r.IsDeleted, ct);

            if (request == null || request.Status == EnumRequestArticleStatus.Draft.ToDbString())
            {
                return Result<RequestArticleResModel>.Error("Request Article not found.");
            }

            if (request.Status == EnumRequestArticleStatus.Approved.ToDbString())
            {
                return Result<RequestArticleResModel>.Error("This Request Article has already been approved and published.");
            }

            if (request.Status != EnumRequestArticleStatus.Submitted.ToDbString())
            {
                return Result<RequestArticleResModel>.Error($"Only articles in 'Submitted' status can be reviewed (current status: '{request.Status}').");
            }

            var now = DateTimeOffset.UtcNow;

            if (normalizedDecision == EnumRequestArticleStatus.Approved.ToDbString())
            {
                // 1. Ensure slug is unique in Tbl_Article
                var slug = request.Slug;
                var isSlugTaken = await _db.TblArticles.AnyAsync(a => a.Slug == slug && !a.IsDeleted, ct);
                if (isSlugTaken)
                {
                    slug = $"{slug}-{Guid.NewGuid().ToString()[..5]}";
                }

                var newArticleId = Ulid.NewUlid().ToString();

                // 2. Create Published Article in Tbl_Article
                var publishedArticle = new TblArticle
                {
                    ArticleId = newArticleId,
                    Title = request.Title,
                    SubTitle = request.SubTitle,
                    Slug = slug,
                    Body = request.Body,
                    PhotoUrl = request.PhotoUrl,
                    PhotoCaption = request.PhotoCaption,
                    PhotoCredit = request.PhotoCredit,
                    ArticleCategoryId = request.ArticleCategoryId,
                    AuthorId = request.AuthorId,
                    Status = EnumArticleStatus.Published.ToDbString(),
                    PublishedAt = now,
                    IsFeatured = req.IsFeatured,
                    IsSpotlight = req.IsSpotlight,
                    CreatedAt = now
                };

                _db.TblArticles.Add(publishedArticle);

                // 3. Link all active gallery photos from Tbl_RequestArticle to the new Tbl_Article
                var galleryPhotos = await _db.TblArticlePhotos
                    .Where(p => p.RequestArticleId == request.RequestArticleId && !p.IsDeleted)
                    .ToListAsync(ct);

                foreach (var photo in galleryPhotos)
                {
                    photo.ArticleId = newArticleId;
                    photo.UpdatedAt = now;
                }

                // 4. Mark Tbl_RequestArticle as Approved (Locked) and link PublishedArticleId
                request.Status = EnumRequestArticleStatus.Approved.ToDbString();
                request.PublishedArticleId = newArticleId;
                request.AdminNotes = req.AdminNotes;
                request.ReviewedAt = now;
                request.ReviewedByAdminId = AuthorizedAdminId;
                request.UpdatedAt = now;
            }
            else
            {
                // Rejected
                request.Status = EnumRequestArticleStatus.Rejected.ToDbString();
                request.AdminNotes = req.AdminNotes;
                request.ReviewedAt = now;
                request.ReviewedByAdminId = AuthorizedAdminId;
                request.UpdatedAt = now;
            }

            await _db.SaveChangesAsync(ct);

            return await GetRequestArticleById(new RequestArticleDetailReqModel { RequestArticleId = request.RequestArticleId }, ct);
        }
        catch (Exception ex)
        {
            return Result<RequestArticleResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error reviewing request article"));
        }
    }

    private static string? NormalizeAdminDecision(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var trimmed = status.Trim();
        if (trimmed.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            return EnumRequestArticleStatus.Approved.ToDbString();

        if (trimmed.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            return EnumRequestArticleStatus.Rejected.ToDbString();

        return null;
    }
}
