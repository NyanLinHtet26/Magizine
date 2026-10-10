using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblNewsletter
{
    public string NewsletterId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    public DateTimeOffset SubscribedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset? UnsubscribedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }
}
