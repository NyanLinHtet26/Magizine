using System.Security.Claims;
using Magizine.Shared.Security;

namespace MagizineAdmin.Api.Features.ArticleCategory;

/// <summary>
/// Provides the current authenticated admin's identity from the JWT.
/// Scoped so it reads from the active HttpContext on each request.
/// </summary>
public sealed class CurrentAdminAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentAdminAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// The numeric admin ID from the <c>sub</c> claim. Throws if not authenticated.
    /// </summary>
    public long AdminId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var sub = user?.FindFirst(MagizineClaims.Subject)?.Value;
            if (!long.TryParse(sub, out var id))
            {
                throw new UnauthorizedAccessException("Admin identity not found in token.");
            }
            return id;
        }
    }

    /// <summary>
    /// The role from the <c>role</c> claim. Throws if not present.
    /// </summary>
    public string Role
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.Role)?.Value;
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new UnauthorizedAccessException("Admin role not found in token.");
            }
            return role;
        }
    }

    /// <summary>
    /// The username from the <c>username</c> claim. Throws if not present.
    /// </summary>
    public string UserName
    {
        get
        {
            var name = _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.UserName)?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new UnauthorizedAccessException("Admin username not found in token.");
            }
            return name;
        }
    }

    /// <summary>
    /// The session ID from the <c>jti</c> claim. Returns null if not present.
    /// </summary>
    public string? SessionId => _httpContextAccessor.HttpContext?.User?.FindFirst(MagizineClaims.SessionId)?.Value;
}