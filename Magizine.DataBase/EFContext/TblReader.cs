using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblReader
{
    public string ReaderId { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public string? PreviousRefreshTokenHash { get; set; }

    public DateTimeOffset? PreviousRefreshTokenExpiresAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }
}
