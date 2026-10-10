using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblAboutAndAppDatum
{
    public string AppDataId { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string? Value { get; set; }

    public string? GroupName { get; set; }

    public string? UpdatedByAdminId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual TblAdmin? UpdatedByAdmin { get; set; }
}
