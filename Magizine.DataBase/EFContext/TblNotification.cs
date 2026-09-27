using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblNotification
{
    public long NotificationId { get; set; }

    public long? RecipientAuthorId { get; set; }

    public long? RecipientAdminId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public string? RelatedEntityType { get; set; }

    public long? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblAdmin? RecipientAdmin { get; set; }

    public virtual TblAuthor? RecipientAuthor { get; set; }
}
