using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblContactMessage
{
    public long ContactMessageId { get; set; }

    public string SenderName { get; set; } = null!;

    public string SenderEmail { get; set; } = null!;

    public string? Subject { get; set; }

    public string MessageBody { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public long? ReadByAdminId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblAdmin? ReadByAdmin { get; set; }
}
