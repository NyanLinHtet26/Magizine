using System.Text.RegularExpressions;
using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Enums;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Reader;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MagizinePublic.Api.Features.Auth;

/// <summary>
/// Internal carrier pairing the public <see cref="ReaderLoginResModel"/> with the raw opaque
/// refresh token that <see cref="AuthController"/> writes into the <c>HttpOnly</c> cookie.
/// </summary>
/// <param name="Response">Public response payload returned inside <see cref="Result{T}.Data"/>.</param>
/// <param name="RawRefreshToken">
/// The new plaintext refresh token to set in the <c>reader_refresh_token</c> <c>HttpOnly</c> cookie,
/// or <c>null</c> when a 30-second grace-period hit preserves the already-rotated cookie.
/// </param>
public sealed record ReaderAuthTokenBundle(ReaderLoginResModel Response, string? RawRefreshToken);

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

    private string? AuthorizedReaderId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value;

    public async Task<Result<ReaderAuthTokenBundle>> RegisterAsync(ReaderRegisterReqModel req, CancellationToken ct = default)
    {
        try
        {
            var email = req.Email.Trim().ToLowerInvariant();

            var isEmailTaken = await _db.TblReaders
                .AnyAsync(r => r.Email == email, ct);

            if (isEmailTaken)
            {
                return Result<ReaderAuthTokenBundle>.Error(
                    JsonResource.Validation,
                    "A reader account with this email address already exists.");
            }

            var firstName = req.FirstName.Trim();
            var lastName = req.LastName.Trim();
            string username;

            if (!string.IsNullOrWhiteSpace(req.Username))
            {
                username = req.Username.Trim().ToLowerInvariant();
                var isUsernameTaken = await _db.TblReaders
                    .AnyAsync(r => r.Username == username, ct);

                if (isUsernameTaken)
                {
                    return Result<ReaderAuthTokenBundle>.Error(
                        JsonResource.Validation,
                        "That username is already taken. Please choose another.");
                }
            }
            else
            {
                username = GenerateBaseUsername(firstName, lastName);
                var isUsernameTaken = await _db.TblReaders
                    .AnyAsync(r => r.Username == username, ct);

                if (isUsernameTaken)
                {
                    username = $"{username}.{Ulid.NewUlid().ToString()[^5..].ToLowerInvariant()}";
                }
            }

            var now = _timeProvider.GetUtcNow();
            var readerId = Ulid.NewUlid().ToString();

            var accessToken = _jwtTokenGenerator.Generate(
                readerId,
                username,
                MagizineClaims.ReaderRole);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            var reader = new TblReader
            {
                ReaderId = readerId,
                FirstName = firstName,
                LastName = lastName,
                Username = username,
                Email = email,
                PasswordHash = _passwordHasher.Hash(req.Password),
                AvatarUrl = req.AvatarUrl?.Trim(),
                Bio = req.Bio?.Trim(),
                RefreshTokenHash = refreshToken.TokenHash,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                PreviousRefreshTokenHash = null,
                PreviousRefreshTokenExpiresAt = null,
                LastLoginAt = now,
                IsActive = true,
                CreatedAt = now
            };

            _db.TblReaders.Add(reader);
            await _db.SaveChangesAsync(ct);

            var response = new ReaderLoginResModel
            {
                AccessToken = accessToken.Token,
                TokenType = "Bearer",
                ExpiresAt = accessToken.ExpiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                SessionId = accessToken.SessionId,
                IsGracePeriodHit = false,
                Reader = MapToProfile(reader)
            };

            return CreateSuccess(
                new ReaderAuthTokenBundle(response, refreshToken.Token),
                "Reader account registered and signed in successfully.");
        }
        catch (Exception ex)
        {
            return Result<ReaderAuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error registering reader '{Email}'", req.Email));
        }
    }

    public async Task<Result<ReaderAuthTokenBundle>> LoginAsync(ReaderLoginReqModel req, CancellationToken ct = default)
    {
        try
        {
            var identifier = req.UsernameOrEmail.Trim().ToLowerInvariant();

            var reader = await _db.TblReaders
                .FirstOrDefaultAsync(
                    r => r.Username == identifier || r.Email == identifier,
                    ct);

            if (reader == null || !_passwordHasher.Verify(req.Password, reader.PasswordHash))
            {
                return Result<ReaderAuthTokenBundle>.Error(
                    JsonResource.Unauthorized,
                    "Invalid username/email or password.");
            }

            if (!reader.IsActive)
            {
                return Result<ReaderAuthTokenBundle>.Error(
                    JsonResource.Forbidden,
                    "Your reader account has been deactivated. Please contact support.");
            }

            var now = _timeProvider.GetUtcNow();

            if (_passwordHasher.NeedsRehash(reader.PasswordHash))
            {
                reader.PasswordHash = _passwordHasher.Hash(req.Password);
            }

            var accessToken = _jwtTokenGenerator.Generate(
                reader.ReaderId,
                reader.Username,
                MagizineClaims.ReaderRole);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            reader.RefreshTokenHash = refreshToken.TokenHash;
            reader.RefreshTokenExpiresAt = refreshToken.ExpiresAt;
            reader.PreviousRefreshTokenHash = null;
            reader.PreviousRefreshTokenExpiresAt = null;
            reader.LastLoginAt = now;
            reader.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            var response = new ReaderLoginResModel
            {
                AccessToken = accessToken.Token,
                TokenType = "Bearer",
                ExpiresAt = accessToken.ExpiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                SessionId = accessToken.SessionId,
                IsGracePeriodHit = false,
                Reader = MapToProfile(reader)
            };

            return CreateSuccess(
                new ReaderAuthTokenBundle(response, refreshToken.Token),
                "Signed in successfully.");
        }
        catch (Exception ex)
        {
            return Result<ReaderAuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error during reader login for '{Identifier}'", req.UsernameOrEmail));
        }
    }

    public async Task<Result<ReaderAuthTokenBundle>> RefreshTokenAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Result<ReaderAuthTokenBundle>.Error(
                    JsonResource.Unauthorized,
                    "Refresh token is missing. Please sign in again.");
            }

            var now = _timeProvider.GetUtcNow();
            var incomingHash = _jwtTokenGenerator.ComputeTokenHash(rawRefreshToken);

            // 1. Primary Lookup: Match active RefreshTokenHash
            var reader = await _db.TblReaders
                .FirstOrDefaultAsync(r => r.RefreshTokenHash == incomingHash, ct);

            if (reader != null)
            {
                if (!reader.IsActive)
                {
                    return Result<ReaderAuthTokenBundle>.Error(
                        JsonResource.Forbidden,
                        "Your reader account is not active.");
                }

                if (reader.RefreshTokenExpiresAt == null || reader.RefreshTokenExpiresAt <= now)
                {
                    reader.RefreshTokenHash = null;
                    reader.RefreshTokenExpiresAt = null;
                    reader.PreviousRefreshTokenHash = null;
                    reader.PreviousRefreshTokenExpiresAt = null;
                    await _db.SaveChangesAsync(ct);

                    return Result<ReaderAuthTokenBundle>.Error(
                        JsonResource.Unauthorized,
                        "Refresh token has expired. Please sign in again.");
                }

                // Standard Rotation: Move current token hash to 30s Grace Window & issue new token pair
                reader.PreviousRefreshTokenHash = reader.RefreshTokenHash;
                reader.PreviousRefreshTokenExpiresAt = now.Add(_jwtOptions.RefreshTokenGracePeriod);

                var newAccess = _jwtTokenGenerator.Generate(
                    reader.ReaderId,
                    reader.Username,
                    MagizineClaims.ReaderRole);
                var newRefresh = _jwtTokenGenerator.GenerateRefreshToken();

                reader.RefreshTokenHash = newRefresh.TokenHash;
                reader.RefreshTokenExpiresAt = newRefresh.ExpiresAt;
                reader.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);

                var rotatedRes = new ReaderLoginResModel
                {
                    AccessToken = newAccess.Token,
                    TokenType = "Bearer",
                    ExpiresAt = newAccess.ExpiresAt,
                    RefreshTokenExpiresAt = newRefresh.ExpiresAt,
                    SessionId = newAccess.SessionId,
                    IsGracePeriodHit = false,
                    Reader = MapToProfile(reader)
                };

                return CreateSuccess(
                    new ReaderAuthTokenBundle(rotatedRes, newRefresh.Token),
                    "Token refreshed successfully.");
            }

            // 2. Fallback Lookup (30s Grace Period Hit for Multi-Tab / Concurrent Race Conditions)
            var graceReader = await _db.TblReaders
                .FirstOrDefaultAsync(
                    r => r.PreviousRefreshTokenHash == incomingHash &&
                         r.PreviousRefreshTokenExpiresAt != null &&
                         r.PreviousRefreshTokenExpiresAt > now,
                    ct);

            if (graceReader != null)
            {
                if (!graceReader.IsActive)
                {
                    return Result<ReaderAuthTokenBundle>.Error(
                        JsonResource.Forbidden,
                        "Your reader account is not active.");
                }

                if (graceReader.RefreshTokenExpiresAt == null || graceReader.RefreshTokenExpiresAt <= now)
                {
                    return Result<ReaderAuthTokenBundle>.Error(
                        JsonResource.Unauthorized,
                        "Active session has expired. Please sign in again.");
                }

                // Grace Period Hit: Issue a fresh AccessToken WITHOUT rotating RefreshTokenHash again
                var graceAccess = _jwtTokenGenerator.Generate(
                    graceReader.ReaderId,
                    graceReader.Username,
                    MagizineClaims.ReaderRole);

                _logger.LogInformation(
                    "Grace period refresh hit for ReaderId {ReaderId} within {GraceSeconds}s leeway window.",
                    graceReader.ReaderId,
                    _jwtOptions.RefreshTokenGracePeriodSeconds);

                var graceRes = new ReaderLoginResModel
                {
                    AccessToken = graceAccess.Token,
                    TokenType = "Bearer",
                    ExpiresAt = graceAccess.ExpiresAt,
                    RefreshTokenExpiresAt = graceReader.RefreshTokenExpiresAt.Value,
                    SessionId = graceAccess.SessionId,
                    IsGracePeriodHit = true,
                    Reader = MapToProfile(graceReader)
                };

                return CreateSuccess(
                    new ReaderAuthTokenBundle(graceRes, RawRefreshToken: null),
                    "Token refreshed via concurrent grace period.");
            }

            return Result<ReaderAuthTokenBundle>.Error(
                JsonResource.Unauthorized,
                "Invalid or expired refresh token. Please sign in again.");
        }
        catch (Exception ex)
        {
            return Result<ReaderAuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error refreshing reader token"));
        }
    }

    public async Task<Result<bool>> LogoutAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            TblReader? reader = null;

            if (!string.IsNullOrWhiteSpace(readerId))
            {
                reader = await _db.TblReaders.FirstOrDefaultAsync(r => r.ReaderId == readerId, ct);
            }

            if (reader == null && !string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                var tokenHash = _jwtTokenGenerator.ComputeTokenHash(rawRefreshToken);
                reader = await _db.TblReaders.FirstOrDefaultAsync(
                    r => r.RefreshTokenHash == tokenHash || r.PreviousRefreshTokenHash == tokenHash,
                    ct);
            }

            if (reader != null)
            {
                reader.RefreshTokenHash = null;
                reader.RefreshTokenExpiresAt = null;
                reader.PreviousRefreshTokenHash = null;
                reader.PreviousRefreshTokenExpiresAt = null;
                reader.UpdatedAt = _timeProvider.GetUtcNow();

                await _db.SaveChangesAsync(ct);
            }

            return CreateSuccess(true, "Logged out successfully.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error during reader logout"));
        }
    }

    public async Task<Result<ReaderProfileResModel>> GetCurrentReaderProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var reader = await _db.TblReaders
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReaderId == readerId, ct);

            if (reader == null)
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.NotExist,
                    "Reader account not found.");
            }

            if (!reader.IsActive)
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.Forbidden,
                    "Your reader account is not active.");
            }

            return Result<ReaderProfileResModel>.Success(MapToProfile(reader));
        }
        catch (Exception ex)
        {
            return Result<ReaderProfileResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error retrieving current reader profile"));
        }
    }

    public async Task<Result<ReaderProfileResModel>> UpdateProfileAsync(UpdateReaderProfileReqModel req, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var reader = await _db.TblReaders
                .FirstOrDefaultAsync(r => r.ReaderId == readerId, ct);

            if (reader == null)
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.NotExist,
                    "Reader account not found.");
            }

            var normalizedUsername = req.Username.Trim().ToLowerInvariant();
            var normalizedEmail = req.Email.Trim().ToLowerInvariant();

            var isUsernameTaken = await _db.TblReaders
                .AnyAsync(r => r.ReaderId != readerId && r.Username == normalizedUsername, ct);

            if (isUsernameTaken)
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.Validation,
                    "Another reader account is already using this username.");
            }

            var isEmailTaken = await _db.TblReaders
                .AnyAsync(r => r.ReaderId != readerId && r.Email == normalizedEmail, ct);

            if (isEmailTaken)
            {
                return Result<ReaderProfileResModel>.Error(
                    JsonResource.Validation,
                    "Another reader account is already using this email address.");
            }

            reader.FirstName = req.FirstName.Trim();
            reader.LastName = req.LastName.Trim();
            reader.Username = normalizedUsername;
            reader.Email = normalizedEmail;
            reader.AvatarUrl = req.AvatarUrl?.Trim();
            reader.Bio = req.Bio?.Trim();
            reader.UpdatedAt = _timeProvider.GetUtcNow();

            await _db.SaveChangesAsync(ct);

            return CreateSuccess(MapToProfile(reader), "Reader profile updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<ReaderProfileResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error updating reader profile"));
        }
    }

    public async Task<Result<bool>> ChangePasswordAsync(ChangeReaderPasswordReqModel req, CancellationToken ct = default)
    {
        try
        {
            var readerId = AuthorizedReaderId;
            if (string.IsNullOrWhiteSpace(readerId))
            {
                return Result<bool>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var reader = await _db.TblReaders
                .FirstOrDefaultAsync(r => r.ReaderId == readerId, ct);

            if (reader == null)
            {
                return Result<bool>.Error(
                    JsonResource.NotExist,
                    "Reader account not found.");
            }

            if (!_passwordHasher.Verify(req.CurrentPassword, reader.PasswordHash))
            {
                return Result<bool>.Error(
                    JsonResource.Validation,
                    "Current password is incorrect.");
            }

            reader.PasswordHash = _passwordHasher.Hash(req.NewPassword);
            reader.RefreshTokenHash = null;
            reader.RefreshTokenExpiresAt = null;
            reader.PreviousRefreshTokenHash = null;
            reader.PreviousRefreshTokenExpiresAt = null;
            reader.UpdatedAt = _timeProvider.GetUtcNow();

            await _db.SaveChangesAsync(ct);

            return CreateSuccess(true, "Password changed successfully. Please sign in again.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error changing reader password"));
        }
    }

    private static ReaderProfileResModel MapToProfile(TblReader reader) => new()
    {
        ReaderId = reader.ReaderId,
        FirstName = reader.FirstName,
        LastName = reader.LastName,
        Username = reader.Username,
        Email = reader.Email,
        AvatarUrl = reader.AvatarUrl,
        Bio = reader.Bio,
        IsActive = reader.IsActive,
        LastLoginAt = reader.LastLoginAt,
        CreatedAt = reader.CreatedAt,
        UpdatedAt = reader.UpdatedAt
    };

    private static string GenerateBaseUsername(string firstName, string lastName)
    {
        var raw = $"{firstName}.{lastName}".ToLowerInvariant().Trim();
        raw = Regex.Replace(raw, @"[^a-z0-9.]+", "");
        raw = Regex.Replace(raw, @"\.{2,}", ".").Trim('.');
        if (raw.Length < 3)
        {
            raw = $"reader.{Ulid.NewUlid().ToString()[^5..].ToLowerInvariant()}";
        }
        return raw.Length > 40 ? raw[..40] : raw;
    }

    private static Result<T> CreateSuccess<T>(T data, string message) => new()
    {
        Data = data,
        RespCode = JsonResource.Success,
        RespDesp = message,
        RespType = EnumRespType.Success
    };
}
