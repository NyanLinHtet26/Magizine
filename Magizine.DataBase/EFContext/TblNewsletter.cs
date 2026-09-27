using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblNewsletter
{
    public long NewsletterId { get; set; }

    public string Email { get; set; } = null!;

    public string? FullName { get; set; }

    public DateTime SubscribedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? UnsubscribedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }
}
