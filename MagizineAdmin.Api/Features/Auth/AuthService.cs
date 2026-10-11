using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Enums;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Models;
using Magizine.Shared.Models.Admin;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MagizineAdmin.Api.Features.Auth;

/// <summary>
/// Internal carrier pairing the public <see cref="AdminLoginResModel"/> with the raw opaque
/// refresh token that <see cref="AuthController"/> writes into the <c>HttpOnly</c> cookie.
/// </summary>
/// <param name="Response">Public response payload returned inside <see cref="Result{T}.Data"/>.</param>
/// <param name="RawRefreshToken">
/// The new plaintext refresh token to set in the <c>admin_refresh_token</c> <c>HttpOnly</c> cookie,
/// or <c>null</c> when a 30-second grace-period hit preserves the already-rotated cookie.
/// </param>
public sealed record AuthTokenBundle(AdminLoginResModel Response, string? RawRefreshToken);

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

    private string? AuthorizedAdminId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Subject)?.Value;

    public async Task<Result<AuthTokenBundle>> LoginAsync(AdminLoginReqModel req, CancellationToken ct = default)
    {
        try
        {
            var identifier = req.UsernameOrEmail.Trim();

            var admin = await _db.TblAdmins
                .FirstOrDefaultAsync(
                    a => a.Username == identifier || a.Email == identifier,
                    ct);

            if (admin == null || !_passwordHasher.Verify(req.Password, admin.PasswordHash))
            {
                return Result<AuthTokenBundle>.Error(
                    JsonResource.Unauthorized,
                    "Invalid username/email or password.");
            }

            if (!admin.IsActive)
            {
                return Result<AuthTokenBundle>.Error(
                    JsonResource.Forbidden,
                    "Your admin account has been deactivated. Please contact a SuperAdmin.");
            }

            var now = _timeProvider.GetUtcNow();

            if (_passwordHasher.NeedsRehash(admin.PasswordHash))
            {
                admin.PasswordHash = _passwordHasher.Hash(req.Password);
            }

            var accessToken = _jwtTokenGenerator.Generate(admin.AdminId, admin.Username, admin.Role);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            admin.RefreshTokenHash = refreshToken.TokenHash;
            admin.RefreshTokenExpiresAt = refreshToken.ExpiresAt;
            admin.PreviousRefreshTokenHash = null;
            admin.PreviousRefreshTokenExpiresAt = null;
            admin.LastLoginAt = now;
            admin.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            var response = new AdminLoginResModel
            {
                AccessToken = accessToken.Token,
                TokenType = "Bearer",
                ExpiresAt = accessToken.ExpiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                SessionId = accessToken.SessionId,
                IsGracePeriodHit = false,
                Admin = MapToProfile(admin)
            };

            return CreateSuccess(new AuthTokenBundle(response, refreshToken.Token), "Signed in successfully.");
        }
        catch (Exception ex)
        {
            return Result<AuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error during admin login for '{Identifier}'", req.UsernameOrEmail));
        }
    }

    public async Task<Result<AuthTokenBundle>> RefreshTokenAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return Result<AuthTokenBundle>.Error(
                    JsonResource.Unauthorized,
                    "Refresh token is missing. Please sign in again.");
            }

            var now = _timeProvider.GetUtcNow();
            var incomingHash = _jwtTokenGenerator.ComputeTokenHash(rawRefreshToken);

            // 1. Primary Lookup: Match active RefreshTokenHash
            var admin = await _db.TblAdmins
                .FirstOrDefaultAsync(a => a.RefreshTokenHash == incomingHash, ct);

            if (admin != null)
            {
                if (!admin.IsActive)
                {
                    return Result<AuthTokenBundle>.Error(
                        JsonResource.Forbidden,
                        "Your admin account has been deactivated.");
                }

                if (admin.RefreshTokenExpiresAt == null || admin.RefreshTokenExpiresAt <= now)
                {
                    admin.RefreshTokenHash = null;
                    admin.RefreshTokenExpiresAt = null;
                    admin.PreviousRefreshTokenHash = null;
                    admin.PreviousRefreshTokenExpiresAt = null;
                    await _db.SaveChangesAsync(ct);

                    return Result<AuthTokenBundle>.Error(
                        JsonResource.Unauthorized,
                        "Refresh token has expired. Please sign in again.");
                }

                // Standard Rotation: Move current token hash to 30s Grace Window & issue new token pair
                admin.PreviousRefreshTokenHash = admin.RefreshTokenHash;
                admin.PreviousRefreshTokenExpiresAt = now.Add(_jwtOptions.RefreshTokenGracePeriod);

                var newAccess = _jwtTokenGenerator.Generate(admin.AdminId, admin.Username, admin.Role);
                var newRefresh = _jwtTokenGenerator.GenerateRefreshToken();

                admin.RefreshTokenHash = newRefresh.TokenHash;
                admin.RefreshTokenExpiresAt = newRefresh.ExpiresAt;
                admin.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);

                var rotatedRes = new AdminLoginResModel
                {
                    AccessToken = newAccess.Token,
                    TokenType = "Bearer",
                    ExpiresAt = newAccess.ExpiresAt,
                    RefreshTokenExpiresAt = newRefresh.ExpiresAt,
                    SessionId = newAccess.SessionId,
                    IsGracePeriodHit = false,
                    Admin = MapToProfile(admin)
                };

                return CreateSuccess(
                    new AuthTokenBundle(rotatedRes, newRefresh.Token),
                    "Token refreshed successfully.");
            }

            // 2. Fallback Lookup (30s Grace Period Hit for Multi-Tab / Concurrent Race Conditions)
            var graceAdmin = await _db.TblAdmins
                .FirstOrDefaultAsync(
                    a => a.PreviousRefreshTokenHash == incomingHash &&
                         a.PreviousRefreshTokenExpiresAt != null &&
                         a.PreviousRefreshTokenExpiresAt > now,
                    ct);

            if (graceAdmin != null)
            {
                if (!graceAdmin.IsActive)
                {
                    return Result<AuthTokenBundle>.Error(
                        JsonResource.Forbidden,
                        "Your admin account has been deactivated.");
                }

                if (graceAdmin.RefreshTokenExpiresAt == null || graceAdmin.RefreshTokenExpiresAt <= now)
                {
                    return Result<AuthTokenBundle>.Error(
                        JsonResource.Unauthorized,
                        "Active session has expired. Please sign in again.");
                }

                // Grace Period Hit: Issue a fresh AccessToken WITHOUT rotating RefreshTokenHash again
                var graceAccess = _jwtTokenGenerator.Generate(graceAdmin.AdminId, graceAdmin.Username, graceAdmin.Role);

                _logger.LogInformation(
                    "Grace period refresh hit for AdminId {AdminId} within {GraceSeconds}s leeway window.",
                    graceAdmin.AdminId,
                    _jwtOptions.RefreshTokenGracePeriodSeconds);

                var graceRes = new AdminLoginResModel
                {
                    AccessToken = graceAccess.Token,
                    TokenType = "Bearer",
                    ExpiresAt = graceAccess.ExpiresAt,
                    RefreshTokenExpiresAt = graceAdmin.RefreshTokenExpiresAt.Value,
                    SessionId = graceAccess.SessionId,
                    IsGracePeriodHit = true,
                    Admin = MapToProfile(graceAdmin)
                };

                return CreateSuccess(
                    new AuthTokenBundle(graceRes, RawRefreshToken: null),
                    "Token refreshed via concurrent grace period.");
            }

            return Result<AuthTokenBundle>.Error(
                JsonResource.Unauthorized,
                "Invalid or expired refresh token. Please sign in again.");
        }
        catch (Exception ex)
        {
            return Result<AuthTokenBundle>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error refreshing admin token"));
        }
    }

    public async Task<Result<bool>> LogoutAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        try
        {
            var adminId = AuthorizedAdminId;
            TblAdmin? admin = null;

            if (!string.IsNullOrWhiteSpace(adminId))
            {
                admin = await _db.TblAdmins.FirstOrDefaultAsync(a => a.AdminId == adminId, ct);
            }

            if (admin == null && !string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                var tokenHash = _jwtTokenGenerator.ComputeTokenHash(rawRefreshToken);
                admin = await _db.TblAdmins.FirstOrDefaultAsync(
                    a => a.RefreshTokenHash == tokenHash || a.PreviousRefreshTokenHash == tokenHash,
                    ct);
            }

            if (admin != null)
            {
                admin.RefreshTokenHash = null;
                admin.RefreshTokenExpiresAt = null;
                admin.PreviousRefreshTokenHash = null;
                admin.PreviousRefreshTokenExpiresAt = null;
                admin.UpdatedAt = _timeProvider.GetUtcNow();

                await _db.SaveChangesAsync(ct);
            }

            return CreateSuccess(true, "Logged out successfully.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error during admin logout"));
        }
    }

    public async Task<Result<AdminProfileResModel>> GetCurrentAdminProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var adminId = AuthorizedAdminId;
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var admin = await _db.TblAdmins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AdminId == adminId, ct);

            if (admin == null)
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.NotExist,
                    "Admin account not found.");
            }

            if (!admin.IsActive)
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.Forbidden,
                    "Your admin account has been deactivated.");
            }

            return Result<AdminProfileResModel>.Success(MapToProfile(admin));
        }
        catch (Exception ex)
        {
            return Result<AdminProfileResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error retrieving current admin profile"));
        }
    }

    public async Task<Result<AdminProfileResModel>> UpdateProfileAsync(UpdateAdminProfileReqModel req, CancellationToken ct = default)
    {
        try
        {
            var adminId = AuthorizedAdminId;
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var admin = await _db.TblAdmins.FirstOrDefaultAsync(a => a.AdminId == adminId, ct);
            if (admin == null)
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.NotExist,
                    "Admin account not found.");
            }

            var username = req.Username.Trim();
            var email = req.Email.Trim();

            var duplicateUsername = await _db.TblAdmins
                .AnyAsync(a => a.AdminId != adminId && a.Username == username, ct);
            if (duplicateUsername)
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.Validation,
                    "Username is already taken by another admin.");
            }

            var duplicateEmail = await _db.TblAdmins
                .AnyAsync(a => a.AdminId != adminId && a.Email == email, ct);
            if (duplicateEmail)
            {
                return Result<AdminProfileResModel>.Error(
                    JsonResource.Validation,
                    "Email is already registered to another admin.");
            }

            admin.Username = username;
            admin.Email = email;
            admin.UpdatedAt = _timeProvider.GetUtcNow();

            await _db.SaveChangesAsync(ct);

            return CreateSuccess(MapToProfile(admin), "Admin profile updated successfully.");
        }
        catch (Exception ex)
        {
            return Result<AdminProfileResModel>.Error(
                ex,
                JsonResource.Fail,
                e => _logger.LogError(e, "Error updating admin profile"));
        }
    }

    public async Task<Result<bool>> ChangePasswordAsync(ChangeAdminPasswordReqModel req, CancellationToken ct = default)
    {
        try
        {
            var adminId = AuthorizedAdminId;
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Result<bool>.Error(
                    JsonResource.Unauthorized,
                    "Authentication is required.");
            }

            var admin = await _db.TblAdmins.FirstOrDefaultAsync(a => a.AdminId == adminId, ct);
            if (admin == null)
            {
                return Result<bool>.Error(
                    JsonResource.NotExist,
                    "Admin account not found.");
            }

            if (!_passwordHasher.Verify(req.CurrentPassword, admin.PasswordHash))
            {
                return Result<bool>.Error(
                    JsonResource.Unauthorized,
                    "Current password is incorrect.");
            }

            if (_passwordHasher.Verify(req.NewPassword, admin.PasswordHash))
            {
                return Result<bool>.Error(
                    JsonResource.Validation,
                    "New password must be different from your current password.");
            }

            admin.PasswordHash = _passwordHasher.Hash(req.NewPassword);
            admin.RefreshTokenHash = null;
            admin.RefreshTokenExpiresAt = null;
            admin.PreviousRefreshTokenHash = null;
            admin.PreviousRefreshTokenExpiresAt = null;
            admin.UpdatedAt = _timeProvider.GetUtcNow();

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
                e => _logger.LogError(e, "Error changing admin password"));
        }
    }

    private static Result<T> CreateSuccess<T>(T data, string respDesp) => new()
    {
        Data = data,
        RespCode = JsonResource.Success,
        RespDesp = respDesp,
        RespType = EnumRespType.Success
    };

    private static AdminProfileResModel MapToProfile(TblAdmin admin) => new()
    {
        AdminId = admin.AdminId,
        Username = admin.Username,
        Email = admin.Email,
        Role = admin.Role,
        IsActive = admin.IsActive,
        LastLoginAt = admin.LastLoginAt,
        CreatedAt = admin.CreatedAt,
        UpdatedAt = admin.UpdatedAt
    };
}
