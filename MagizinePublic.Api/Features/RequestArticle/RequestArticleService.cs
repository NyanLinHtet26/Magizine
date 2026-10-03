using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Models;
using Magizine.Shared.Models.RequestArticle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizinePublic.Api.Features.RequestArticle;

public sealed class RequestArticleService
{
    private readonly MagizineDbContext _db;
    private readonly ILogger<RequestArticleService> _logger;

    public RequestArticleService(MagizineDbContext db, ILogger<RequestArticleService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<string>> SubmitPitchAsync(CreateRequestArticleReqModel req, CancellationToken ct = default)
    {
        try
        {
            if (req.ArticleCategoryId.HasValue)
            {
                var categoryExists = await _db.TblArticleCategories
                    .AnyAsync(c => c.ArticleCategoryId == req.ArticleCategoryId.Value && !c.IsDeleted, ct);
                
                if (!categoryExists)
                {
                    return Result<string>.Error("The selected article category does not exist.");
                }
            }

            var request = new TblRequestArticle
            {
                PitcherName = req.PitcherName.Trim(),
                PitcherEmail = req.PitcherEmail.Trim(),
                ProposedTitle = req.ProposedTitle?.Trim(),
                PitchBody = req.PitchBody,
                ArticleCategoryId = req.ArticleCategoryId,
                Status = "Pending", // Default status for new pitches
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.TblRequestArticles.Add(request);
            await _db.SaveChangesAsync(ct);

            return Result<string>.Success("Your pitch has been successfully submitted. We will review it shortly.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error submitting request article pitch"));
        }
    }
}
