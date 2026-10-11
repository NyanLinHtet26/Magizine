using System.ComponentModel.DataAnnotations;

namespace Magizine.Shared.Models.Admin;

public sealed class AdminLoginReqModel
{
    [Required(ErrorMessage = "Username or email is required.")]
    [StringLength(100, ErrorMessage = "Username or email cannot exceed 100 characters.")]
    public string UsernameOrEmail { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(72, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 72 characters.")]
    public string Password { get; set; } = null!;
}

public sealed class RefreshTokenReqModel
{
    /// <summary>
    /// Optional fallback for non-cookie API clients. Browser clients send the <c>admin_refresh_token</c> HttpOnly cookie automatically.
    /// </summary>
    public string? RefreshToken { get; set; }
}

public sealed class AdminLoginResModel
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public string SessionId { get; set; } = null!;
    public bool IsGracePeriodHit { get; set; }
    public AdminProfileResModel Admin { get; set; } = null!;
}

public sealed class AdminProfileResModel
{
    public string AdminId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ChangeAdminPasswordReqModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [StringLength(72)]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 72 characters.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Confirmation password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation password do not match.")]
    public string ConfirmNewPassword { get; set; } = null!;
}

public sealed class UpdateAdminProfileReqModel
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;
}
