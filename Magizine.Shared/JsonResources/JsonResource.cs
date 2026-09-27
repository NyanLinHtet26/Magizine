namespace Magizine.Shared.JsonResources;

/// <summary>
/// Central catalogue of every <c>RespCode</c> a <see cref="Models.Result{T}"/> can carry.
/// </summary>
/// <remarks>
/// One place to grep for message codes instead of scattering string literals across services.
/// Codes follow the convention <c>MS#</c> success, <c>ME#</c> error, <c>MW#</c> warning.
/// Keep this file small and current - a code with no documentation is worse than a literal.
/// </remarks>
public static class JsonResource
{
    #region General Response

    /// <summary>Success.</summary>
    public const string Success = "MS#000";

    /// <summary>System error. The generic fallback; never carries exception detail.</summary>
    public const string Fail = "ME#999";

    /// <summary>Requested data does not exist.</summary>
    public const string NotExist = "ME#000";

    #endregion

    #region Request Validation

    /// <summary>
    /// Caller input failed a presence or format check, so the request became HTTP 400. The
    /// per-field messages are in the response's <c>Errors</c> map, not in <c>RespDesp</c>.
    /// </summary>
    /// <remarks>
    /// Different from a business-rule failure: "title is required" is validation, but
    /// "that slug is already taken" is a legitimate answer at HTTP 200 and belongs in
    /// <c>Result&lt;T&gt;.Error</c> with a specific code.
    /// </remarks>
    public const string Validation = "ME#400";

    #endregion

    #region Message Warning

    /// <summary>A required field was not supplied.</summary>
    public const string MW001 = "MW#001";

    #endregion
}
