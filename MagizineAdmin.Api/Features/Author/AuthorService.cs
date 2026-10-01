using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.DataBase.Paging;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Author;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MagizineAdmin.Api.Features.Author;

public sealed class AuthorService
{
    private readonly MagizineDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<AuthorService> _logger;

    // TODO: Extract from HttpContext when Auth is fully active
    private long AuthorizedAdminId => 1; 

    public AuthorService(
        MagizineDbContext db,
        PasswordHasher passwordHasher,
        ILogger<AuthorService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AuthorResModel>>> GetAuthorList(AuthorListReqModel req, CancellationToken ct = default)
    {
        try
        {
            var query = _db.TblAuthors.Where(a => !a.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.SearchKeyword))
            {
                var keyword = req.SearchKeyword.Trim().ToLower();
                query = query.Where(a => a.FirstName.ToLower().Contains(keyword) 
                                      || a.LastName.ToLower().Contains(keyword) 
                                      || a.Email.ToLower().Contains(keyword));
            }

            if (req.IsApproved.HasValue)
            {
                query = query.Where(a => a.IsApproved == req.IsApproved.Value);
            }

            if (req.IsActive.HasValue)
            {
                query = query.Where(a => a.IsActive == req.IsActive.Value);
            }

            var pageReq = PageRequest.Create(req.Page, req.PageSize);
            
            var result = await query.ToPagedResultAsync(
                a => new AuthorResModel
                {
                    AuthorId = a.AuthorId,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    Title = a.Title,
                    Bio = a.Bio,
                    PhotoUrl = a.PhotoUrl,
                    InstagramUrl = a.InstagramUrl,
                    TwitterUrl = a.TwitterUrl,
                    WebsiteUrl = a.WebsiteUrl,
                    Slug = a.Slug,
                    IsApproved = a.IsApproved,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                },
                pageReq,
                ct);

            return Result<PagedResult<AuthorResModel>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<AuthorResModel>>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving author list"));
        }
    }

    public async Task<Result<AuthorResModel>> GetAuthorById(AuthorDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var author = await _db.TblAuthors
                .Where(a => !a.IsDeleted && a.AuthorId == req.AuthorId)
                .Select(a => new AuthorResModel
                {
                    AuthorId = a.AuthorId,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    Title = a.Title,
                    Bio = a.Bio,
                    PhotoUrl = a.PhotoUrl,
                    InstagramUrl = a.InstagramUrl,
                    TwitterUrl = a.TwitterUrl,
                    WebsiteUrl = a.WebsiteUrl,
                    Slug = a.Slug,
                    IsApproved = a.IsApproved,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (author == null)
            {
                return Result<AuthorResModel>.Error("Author not found.");
            }

            return Result<AuthorResModel>.Success(author);
        }
        catch (Exception ex)
        {
            return Result<AuthorResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error retrieving author"));
        }
    }

    public async Task<Result<AuthorResModel>> CreateAuthor(CreateAuthorReqModel req, CancellationToken ct = default)
    {
        try
        {
            var isEmailTaken = await _db.TblAuthors.AnyAsync(a => a.Email.ToLower() == req.Email.ToLower() && !a.IsDeleted, ct);
            if (isEmailTaken)
            {
                return Result<AuthorResModel>.Error("Email is already in use.");
            }

            var slug = Slugify($"{req.FirstName} {req.LastName}");
            var isSlugTaken = await _db.TblAuthors.AnyAsync(a => a.Slug == slug && !a.IsDeleted, ct);
            if (isSlugTaken)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            var author = new TblAuthor
            {
                FirstName = req.FirstName.Trim(),
                LastName = req.LastName.Trim(),
                Email = req.Email.Trim().ToLower(),
                PasswordHash = _passwordHasher.Hash(req.Password),
                Title = req.Title,
                Bio = req.Bio,
                PhotoUrl = req.PhotoUrl,
                InstagramUrl = req.InstagramUrl,
                TwitterUrl = req.TwitterUrl,
                WebsiteUrl = req.WebsiteUrl,
                Slug = slug,
                IsApproved = true, // Auto-approve if created by admin
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.TblAuthors.Add(author);
            await _db.SaveChangesAsync(ct);

            return await GetAuthorById(new AuthorDetailReqModel { AuthorId = author.AuthorId }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result<AuthorResModel>.Error("An author with this email or slug already exists.");
        }
        catch (Exception ex)
        {
            return Result<AuthorResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error creating author"));
        }
    }

    public async Task<Result<AuthorResModel>> UpdateAuthor(UpdateAuthorReqModel req, CancellationToken ct = default)
    {
        try
        {
            var author = await _db.TblAuthors
                .FirstOrDefaultAsync(a => a.AuthorId == req.AuthorId && !a.IsDeleted, ct);

            if (author == null)
            {
                return Result<AuthorResModel>.Error("Author not found.");
            }

            if (author.Email.ToLower() != req.Email.ToLower())
            {
                var isEmailTaken = await _db.TblAuthors.AnyAsync(a => a.Email.ToLower() == req.Email.ToLower() && !a.IsDeleted, ct);
                if (isEmailTaken)
                {
                    return Result<AuthorResModel>.Error("Email is already in use.");
                }
            }

            author.FirstName = req.FirstName.Trim();
            author.LastName = req.LastName.Trim();
            author.Email = req.Email.Trim().ToLower();
            author.Title = req.Title;
            author.Bio = req.Bio;
            author.PhotoUrl = req.PhotoUrl;
            author.InstagramUrl = req.InstagramUrl;
            author.TwitterUrl = req.TwitterUrl;
            author.WebsiteUrl = req.WebsiteUrl;
            author.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return await GetAuthorById(new AuthorDetailReqModel { AuthorId = author.AuthorId }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Result<AuthorResModel>.Error("Email or Slug unique constraint violation.");
        }
        catch (Exception ex)
        {
            return Result<AuthorResModel>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating author"));
        }
    }

    public async Task<Result<string>> ChangeAuthorStatus(ChangeAuthorStatusReqModel req, CancellationToken ct = default)
    {
        try
        {
            var author = await _db.TblAuthors
                .FirstOrDefaultAsync(a => a.AuthorId == req.AuthorId && !a.IsDeleted, ct);

            if (author == null)
            {
                return Result<string>.Error("Author not found.");
            }

            author.IsApproved = req.IsApproved;
            author.IsActive = req.IsActive;
            author.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Author status updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error updating author status"));
        }
    }

    public async Task<Result<string>> DeleteAuthor(AuthorDetailReqModel req, CancellationToken ct = default)
    {
        try
        {
            var author = await _db.TblAuthors
                .FirstOrDefaultAsync(a => a.AuthorId == req.AuthorId && !a.IsDeleted, ct);

            if (author == null)
            {
                return Result<string>.Error("Author not found.");
            }

            author.IsDeleted = true;
            author.DeletedAt = DateTime.UtcNow;
            
            var adminId = AuthorizedAdminId;
            if (adminId > 0)
            {
                author.DeletedByAdminId = adminId;
            }

            await _db.SaveChangesAsync(ct);
            return Result<string>.Success("Author deleted successfully.");
        }
        catch (Exception ex)
        {
            return Result<string>.Error(ex, "ME#999", e => _logger.LogError(e, "Error deleting author"));
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";

    private static string Slugify(string input)
    {
        var result = new System.Text.StringBuilder(input.Length);
        bool prevWasHyphen = true;

        foreach (var c in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(c);
                prevWasHyphen = false;
            }
            else if (!prevWasHyphen)
            {
                result.Append('-');
                prevWasHyphen = true;
            }
        }

        while (result.Length > 0 && result[^1] == '-')
        {
            result.Remove(result.Length - 1, 1);
        }

        return result.Length == 0 ? "author" : result.ToString();
    }
}
