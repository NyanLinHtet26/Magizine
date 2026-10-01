using Magizine.DataBase;
using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Magizine.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAuthor.Api.Features.ArticleCategory;

public sealed class ArticleCategoryService
{
    private readonly MagizineDbContext _db;
    private readonly DapperService _dapperService;
    private readonly ILogger<ArticleCategoryService> _logger;

    public ArticleCategoryService(MagizineDbContext db, DapperService dapperService, ILogger<ArticleCategoryService> logger)
    {
        _db = db;
        _dapperService = dapperService;
        _logger = logger;
    }

    public async Task<Result<List<ArticleCategoryResModel>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var parameters = new
            {
                p_search_keyword = (string?)null,
                p_is_active = true,
                p_page = 1,
                p_page_size = 1000
            };

            var categories = await _dapperService.GetListAsync<ArticleCategoryResModel>(
                "fn_get_article_category_list",
                parameters);

            return Result<List<ArticleCategoryResModel>>.Success(categories);
        }
        catch (Exception ex)
        {
            return Result<List<ArticleCategoryResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving categories for author via Dapper"));
        }
    }
}
