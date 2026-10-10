using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblContactMessage
{
    public string ContactMessageId { get; set; } = null!;

    public string SenderName { get; set; } = null!;

    public string SenderEmail { get; set; } = null!;

    public string? Subject { get; set; }

    public string MessageBody { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public string? ReadByAdminId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblAdmin? ReadByAdmin { get; set; }
}
