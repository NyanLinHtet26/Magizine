using System.ComponentModel;

namespace Magizine.Shared.Enums;

/// <summary>
/// Broad outcome category carried on a <see cref="Models.Result{T}"/>.
/// </summary>
public enum EnumRespType
{
    [Description("None")] None,
    [Description("Success")] Success,
    // [Description("Information")] MI,
    [Description("Warning")] Warning,
    [Description("Error")] Error,
    [Description("System Error")] SystemError
}
