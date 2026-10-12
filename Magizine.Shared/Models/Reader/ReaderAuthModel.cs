using System.ComponentModel.DataAnnotations;

namespace Magizine.Shared.Models.Reader;

public sealed class ReaderRegisterReqModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Optional handle. If omitted, a unique handle is auto-generated from <see cref="FirstName"/> and <see cref="LastName"/>.
    /// </summary>
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 72 characters.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Confirmation password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = null!;

    [StringLength(300)]
    public string? AvatarUrl { get; set; }

    [StringLength(300)]
    public string? Bio { get; set; }
}

public sealed class ReaderLoginReqModel
{
    [Required(ErrorMessage = "Username or email is required.")]
    [StringLength(100, ErrorMessage = "Username or email cannot exceed 100 characters.")]
    public string UsernameOrEmail { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(72, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 72 characters.")]
    public string Password { get; set; } = null!;
}

public sealed class ReaderRefreshTokenReqModel
{
    /// <summary>
    /// Optional fallback for non-cookie API clients. Browser clients send the <c>reader_refresh_token</c> HttpOnly cookie automatically.
    /// </summary>
    public string? RefreshToken { get; set; }
}

public sealed class ReaderProfileResModel
{
    public string ReaderId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ReaderLoginResModel
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public string SessionId { get; set; } = null!;
    public bool IsGracePeriodHit { get; set; }
    public ReaderProfileResModel Reader { get; set; } = null!;
}

public sealed class UpdateReaderProfileReqModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(300)]
    public string? AvatarUrl { get; set; }

    [StringLength(300)]
    public string? Bio { get; set; }
}

public sealed class ChangeReaderPasswordReqModel
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
