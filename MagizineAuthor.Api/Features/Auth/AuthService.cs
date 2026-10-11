using System.Text.RegularExpressions;
using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Enums;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Author;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MagizineAuthor.Api.Features.Auth;

/// <summary>
/// Internal carrier pairing the public <see cref="AuthorLoginResModel"/> with the raw opaque
/// refresh token that <see cref="AuthController"/> writes into the <c>HttpOnly</c> cookie.
/// </summary>
/// <param name="Response">Public response payload returned inside <see cref="Result{T}.Data"/>.</param>
/// <param name="RawRefreshToken">
/// The new plaintext refresh token to set in the <c>author_refresh_token</c> <c>HttpOnly</c> cookie,
/// or <c>null</c> when a 30-second grace-period hit preserves the already-rotated cookie.
/// </param>
public sealed record AuthorAuthTokenBundle(AuthorLoginResModel Response, string? RawRefreshToken);

public sealed class AuthService
{
    private readonly MagizineDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        MagizineDbContext db,
        PasswordHasher passwordHasher,
        JwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string? AuthorizedAuthorId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value;

    public async Task<Result<AuthorResModel>> RegisterAsync(AuthorRegisterReqModel req, CancellationToken ct = default)
    {
        try
        {
            var email = req.Email.Trim().ToLowerInvariant();

            var isEmailTaken = await _db.TblAuthors
                .AnyAsync(a => a.Email == email, ct);

            if (isEmailTaken)
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.Validation,
                    "An author account with this email address already exists.");
            }

            var firstName = req.FirstName.Trim();
            var lastName = req.LastName.Trim();
            var slug = Slugify($"{firstName} {lastName}");

            var isSlugTaken = await _db.TblAuthors.AnyAsync(a => a.Slug == slug, ct);
            if (isSlugTaken)
            {
                slug = $"{slug}-{Ulid.NewUlid().ToString()[^5..].ToLowerInvariant()}";
            }

            var now = _timeProvider.GetUtcNow();

            var author = new TblAuthor
            {
                AuthorId = Ulid.NewUlid().ToString(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = _passwordHasher.Hash(req.Password),
                Title = req.Title?.Trim(),
                Bio = req.Bio?.Trim(),
                PhotoUrl = req.PhotoUrl?.Trim(),
                InstagramUrl = req.InstagramUrl?.Trim(),
                TwitterUrl = req.TwitterUrl?.Trim(),
                WebsiteUrl = req.WebsiteUrl?.Trim(),
                Slug = slug,
                IsApproved = false, // Requires Admin editorial approval before login
                IsActive = true,
                CreatedAt = now
            };

            _db.TblAuthors.Add(author);
            await _db.SaveChangesAsync(ct);

            return CreateSuccess(
                MapToProfile(author),
                "Author registration submitted successfully. Your account is pending editorial approval by an administrator.");
        }
        catch (Exception ex)
        {
            return Result<AuthorResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error registering author '{Email}'", req.Email));
        }
    }

    public async Task<Result<AuthorAuthTokenBundle>> LoginAsync(AuthorLoginReqModel req, CancellationToken ct = default)
    {
        try
        {
            var identifier = req.EmailOrSlug.Trim().ToLowerInvariant();

            var author = await _db.TblAuthors
                .FirstOrDefaultAsync(
                    a => a.Email == identifier || a.Slug == identifier,
                    ct);

            if (author == null || !_passwordHasher.Verify(req.Password, author.PasswordHash))
            {
                return Result<AuthorAuthTokenBundle>.Error(
                    JsonResource.Unauthorized,
                    "Invalid email/slug or password.");
            }

            if (!author.IsApproved)
            {
                return Result<AuthorAuthTokenBundle>.Error(
                    JsonResource.Forbidden,
                    "Your author account is pending editorial approval by an administrator.");
            }

            if (!author.IsActive)
            {
                return Result<AuthorAuthTokenBundle>.Error(
                    JsonResource.Forbidden,
                    "Your author account has been deactivated. Please contact the editorial team.");
            }

            var now = _timeProvider.GetUtcNow();

            if (_passwordHasher.NeedsRehash(author.PasswordHash))
            {
                author.PasswordHash = _passwordHasher.Hash(req.Password);
            }

            var displayName = $"{author.FirstName} {author.LastName}".Trim();
            var accessToken = _jwtTokenGenerator.Generate(
                author.AuthorId,
                displayName,
                MagizineClaims.AuthorRole);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            author.RefreshTokenHash = refreshToken.TokenHash;
            author.RefreshTokenExpiresAt = refreshToken.ExpiresAt;
            author.PreviousRefreshTokenHash = null;
            author.PreviousRefreshTokenExpiresAt = null;
            author.LastLoginAt = now;
            author.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            var response = new AuthorLoginResModel
            {
                AccessToken = accessToken.Token,
                TokenType = "Bearer",
                ExpiresAt = accessToken.ExpiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                SessionId = accessToken.SessionId,
                IsGracePeriodHit = false,
                Author = MapToProfile(author)
            };

            return CreateSuccess(
                new AuthorAuthTokenBundle(response, refreshToken.Token),
                "Signed in successfully.");
        }
        catch (Exception ex)
        {
            return Result<AuthorAuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error during author login for '{Identifier}'", req.EmailOrSlug));
        }
    }

    public async Task<Result<AuthorAuthTokenBundle>> RefreshTokenAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Result<AuthorAuthTokenBundle>.Error(
                    JsonResource.Unauthorized,
                    "Refresh token is missing. Please sign in again.");
            }

            var now = _timeProvider.GetUtcNow();
            var incomingHash = _jwtTokenGenerator.ComputeTokenHash(rawRefreshToken);

            // 1. Primary Lookup: Match active RefreshTokenHash
            var author = await _db.TblAuthors
                .FirstOrDefaultAsync(a => a.RefreshTokenHash == incomingHash, ct);

            if (author != null)
            {
                if (!author.IsApproved || !author.IsActive)
                {
                    return Result<AuthorAuthTokenBundle>.Error(
                        JsonResource.Forbidden,
                        "Your author account is not active or approved.");
                }

                if (author.RefreshTokenExpiresAt == null || author.RefreshTokenExpiresAt <= now)
                {
                    author.RefreshTokenHash = null;
                    author.RefreshTokenExpiresAt = null;
                    author.PreviousRefreshTokenHash = null;
                    author.PreviousRefreshTokenExpiresAt = null;
                    await _db.SaveChangesAsync(ct);

                    return Result<AuthorAuthTokenBundle>.Error(
                        JsonResource.Unauthorized,
                        "Refresh token has expired. Please sign in again.");
                }

                // Standard Rotation: Move current token hash to 30s Grace Window & issue new token pair
                author.PreviousRefreshTokenHash = author.RefreshTokenHash;
                author.PreviousRefreshTokenExpiresAt = now.Add(_jwtOptions.RefreshTokenGracePeriod);

                var displayName = $"{author.FirstName} {author.LastName}".Trim();
                var newAccess = _jwtTokenGenerator.Generate(
                    author.AuthorId,
                    displayName,
                    MagizineClaims.AuthorRole);
                var newRefresh = _jwtTokenGenerator.GenerateRefreshToken();

                author.RefreshTokenHash = newRefresh.TokenHash;
                author.RefreshTokenExpiresAt = newRefresh.ExpiresAt;
                author.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);

                var rotatedRes = new AuthorLoginResModel
                {
                    AccessToken = newAccess.Token,
                    TokenType = "Bearer",
                    ExpiresAt = newAccess.ExpiresAt,
                    RefreshTokenExpiresAt = newRefresh.ExpiresAt,
                    SessionId = newAccess.SessionId,
                    IsGracePeriodHit = false,
                    Author = MapToProfile(author)
                };

                return CreateSuccess(
                    new AuthorAuthTokenBundle(rotatedRes, newRefresh.Token),
                    "Token refreshed successfully.");
            }

            // 2. Fallback Lookup (30s Grace Period Hit for Multi-Tab / Concurrent Race Conditions)
            var graceAuthor = await _db.TblAuthors
                .FirstOrDefaultAsync(
                    a => a.PreviousRefreshTokenHash == incomingHash &&
                         a.PreviousRefreshTokenExpiresAt != null &&
                         a.PreviousRefreshTokenExpiresAt > now,
                    ct);

            if (graceAuthor != null)
            {
                if (!graceAuthor.IsApproved || !graceAuthor.IsActive)
                {
                    return Result<AuthorAuthTokenBundle>.Error(
                        JsonResource.Forbidden,
                        "Your author account is not active or approved.");
                }

                if (graceAuthor.RefreshTokenExpiresAt == null || graceAuthor.RefreshTokenExpiresAt <= now)
                {
                    return Result<AuthorAuthTokenBundle>.Error(
                        JsonResource.Unauthorized,
                        "Active session has expired. Please sign in again.");
                }

                // Grace Period Hit: Issue a fresh AccessToken WITHOUT rotating RefreshTokenHash again
                var displayName = $"{graceAuthor.FirstName} {graceAuthor.LastName}".Trim();
                var graceAccess = _jwtTokenGenerator.Generate(
                    graceAuthor.AuthorId,
                    displayName,
                    MagizineClaims.AuthorRole);

                _logger.LogInformation(
                    "Grace period refresh hit for AuthorId {AuthorId} within {GraceSeconds}s leeway window.",
                    graceAuthor.AuthorId,
                    _jwtOptions.RefreshTokenGracePeriodSeconds);

                var graceRes = new AuthorLoginResModel
                {
                    AccessToken = graceAccess.Token,
                    TokenType = "Bearer",
                    ExpiresAt = graceAccess.ExpiresAt,
                    RefreshTokenExpiresAt = graceAuthor.RefreshTokenExpiresAt.Value,
                    SessionId = graceAccess.SessionId,
                    IsGracePeriodHit = true,
                    Author = MapToProfile(graceAuthor)
                };

                return CreateSuccess(
                    new AuthorAuthTokenBundle(graceRes, RawRefreshToken: null),
                    "Token refreshed via concurrent grace period.");
            }

            return Result<AuthorAuthTokenBundle>.Error(
                JsonResource.Unauthorized,
                "Invalid or expired refresh token. Please sign in again.");
        }
        catch (Exception ex)
        {
            return Result<AuthorAuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error refreshing author token"));
        }
    }

    public async Task<Result<bool>> LogoutAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        try
        {
            var authorId = AuthorizedAuthorId;
            TblAuthor? author = null;

            if (!string.IsNullOrWhiteSpace(authorId))
            {
                author = await _db.TblAuthors.FirstOrDefaultAsync(a => a.AuthorId == authorId, ct);
            }

            if (author == null && !string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                var tokenHash = _jwtTokenGenerator.ComputeTokenHash(rawRefreshToken);
                author = await _db.TblAuthors.FirstOrDefaultAsync(
                    a => a.RefreshTokenHash == tokenHash || a.PreviousRefreshTokenHash == tokenHash,
                    ct);
            }

            if (author != null)
            {
                author.RefreshTokenHash = null;
                author.RefreshTokenExpiresAt = null;
                author.PreviousRefreshTokenHash = null;
                author.PreviousRefreshTokenExpiresAt = null;
                author.UpdatedAt = _timeProvider.GetUtcNow();

                await _db.SaveChangesAsync(ct);
            }

            return CreateSuccess(true, "Logged out successfully.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error during author logout"));
        }
    }

    public async Task<Result<AuthorResModel>> GetCurrentAuthorProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var authorId = AuthorizedAuthorId;
            if (string.IsNullOrWhiteSpace(authorId))
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var author = await _db.TblAuthors
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AuthorId == authorId, ct);

            if (author == null)
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.NotExist,
                    "Author account not found.");
            }

            if (!author.IsApproved || !author.IsActive)
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.Forbidden,
                    "Your author account is not active or approved.");
            }

            return Result<AuthorResModel>.Success(MapToProfile(author));
        }
        catch (Exception ex)
        {
            return Result<AuthorResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error retrieving current author profile"));
        }
    }

    public async Task<Result<AuthorResModel>> UpdateProfileAsync(UpdateAuthorProfileReqModel req, CancellationToken ct = default)
    {
        try
        {
            var authorId = AuthorizedAuthorId;
            if (string.IsNullOrWhiteSpace(authorId))
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var author = await _db.TblAuthors.FirstOrDefaultAsync(a => a.AuthorId == authorId, ct);
            if (author == null)
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.NotExist,
                    "Author account not found.");
            }

            var email = req.Email.Trim().ToLowerInvariant();
            var duplicateEmail = await _db.TblAuthors
                .AnyAsync(a => a.AuthorId != authorId && a.Email == email, ct);

            if (duplicateEmail)
            {
                return Result<AuthorResModel>.Error(
                    JsonResource.Validation,
                    "Email is already registered to another author.");
            }

            author.FirstName = req.FirstName.Trim();
            author.LastName = req.LastName.Trim();
            author.Email = email;
            author.Title = req.Title?.Trim();
            author.Bio = req.Bio?.Trim();
            author.PhotoUrl = req.PhotoUrl?.Trim();
            author.InstagramUrl = req.InstagramUrl?.Trim();
            author.TwitterUrl = req.TwitterUrl?.Trim();
            author.WebsiteUrl = req.WebsiteUrl?.Trim();
            author.UpdatedAt = _timeProvider.GetUtcNow();

            await _db.SaveChangesAsync(ct);

            return CreateSuccess(MapToProfile(author), "Author profile updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<AuthorResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error updating author profile"));
        }
    }

    public async Task<Result<bool>> ChangePasswordAsync(ChangeAuthorPasswordReqModel req, CancellationToken ct = default)
    {
        try
        {
            var authorId = AuthorizedAuthorId;
            if (string.IsNullOrWhiteSpace(authorId))
            {
                return Result<bool>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var author = await _db.TblAuthors.FirstOrDefaultAsync(a => a.AuthorId == authorId, ct);
            if (author == null)
            {
                return Result<bool>.Error(
                    JsonResource.NotExist,
                    "Author account not found.");
            }

            if (!_passwordHasher.Verify(req.CurrentPassword, author.PasswordHash))
            {
                return Result<bool>.Error(
                    JsonResource.Unauthorized,
                    "Current password is incorrect.");
            }

            if (_passwordHasher.Verify(req.NewPassword, author.PasswordHash))
            {
                return Result<bool>.Error(
                    JsonResource.Validation,
                    "New password must be different from your current password.");
            }

            author.PasswordHash = _passwordHasher.Hash(req.NewPassword);
            author.RefreshTokenHash = null;
            author.RefreshTokenExpiresAt = null;
            author.PreviousRefreshTokenHash = null;
            author.PreviousRefreshTokenExpiresAt = null;
            author.UpdatedAt = _timeProvider.GetUtcNow();

            await _db.SaveChangesAsync(ct);

            return CreateSuccess(
                true,
                "Password changed successfully. Please sign in again with your new password.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error changing author password"));
        }
    }

    private static Result<T> CreateSuccess<T>(T data, string respDesp) => new()
    {
        Data = data,
        RespCode = JsonResource.Success,
        RespDesp = respDesp,
        RespType = EnumRespType.Success
    };

    private static AuthorResModel MapToProfile(TblAuthor author) => new()
    {
        AuthorId = author.AuthorId,
        FirstName = author.FirstName,
        LastName = author.LastName,
        Email = author.Email,
        Title = author.Title,
        Bio = author.Bio,
        PhotoUrl = author.PhotoUrl,
        InstagramUrl = author.InstagramUrl,
        TwitterUrl = author.TwitterUrl,
        WebsiteUrl = author.WebsiteUrl,
        Slug = author.Slug,
        IsApproved = author.IsApproved,
        IsActive = author.IsActive,
        LastLoginAt = author.LastLoginAt,
        CreatedAt = author.CreatedAt,
        UpdatedAt = author.UpdatedAt
    };

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.ToLowerInvariant().Trim();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");
        normalized = Regex.Replace(normalized, @"[\s-]+", "-");
        return normalized.Trim('-');
    }
}
