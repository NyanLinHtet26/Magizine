using System.ComponentModel.DataAnnotations;

namespace Magizine.Shared.Models.Author;

public sealed class AuthorRegisterReqModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = null!;

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

    [StringLength(100)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [StringLength(300)]
    public string? PhotoUrl { get; set; }

    [StringLength(300)]
    public string? InstagramUrl { get; set; }

    [StringLength(300)]
    public string? TwitterUrl { get; set; }

    [StringLength(300)]
    public string? WebsiteUrl { get; set; }
}

public sealed class AuthorLoginReqModel
{
    [Required(ErrorMessage = "Email or slug is required.")]
    [StringLength(100, ErrorMessage = "Email or slug cannot exceed 100 characters.")]
    public string EmailOrSlug { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(72, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 72 characters.")]
    public string Password { get; set; } = null!;
}

public sealed class AuthorRefreshTokenReqModel
{
    /// <summary>
    /// Optional fallback for non-cookie API clients. Browser clients send the <c>author_refresh_token</c> HttpOnly cookie automatically.
    /// </summary>
    public string? RefreshToken { get; set; }
}

public sealed class AuthorLoginResModel
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public string SessionId { get; set; } = null!;
    public bool IsGracePeriodHit { get; set; }
    public AuthorResModel Author { get; set; } = null!;
}

public sealed class UpdateAuthorProfileReqModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(100)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [StringLength(300)]
    public string? PhotoUrl { get; set; }

    [StringLength(300)]
    public string? InstagramUrl { get; set; }

    [StringLength(300)]
    public string? TwitterUrl { get; set; }

    [StringLength(300)]
    public string? WebsiteUrl { get; set; }
}

public sealed class ChangeAuthorPasswordReqModel
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
