using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblAboutAndAppDatum
{
    public long AppDataId { get; set; }

    public string Key { get; set; } = null!;

    public string? Value { get; set; }

    public string? GroupName { get; set; }

    public long? UpdatedByAdminId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual TblAdmin? UpdatedByAdmin { get; set; }
}
