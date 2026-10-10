using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblNotification
{
    public string NotificationId { get; set; } = null!;

    public string? RecipientAuthorId { get; set; }

    public string? RecipientAdminId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public string? RelatedEntityType { get; set; }

    public string? RelatedEntityId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblAdmin? RecipientAdmin { get; set; }

    public virtual TblAuthor? RecipientAuthor { get; set; }
}
