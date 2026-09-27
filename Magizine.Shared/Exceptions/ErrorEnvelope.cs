using Magizine.Shared.Enums;

namespace Magizine.Shared.Exceptions;

/// <summary>
/// The single wire shape for every failed request that reaches
/// <c>IExceptionHandler</c>. Mirrors <see cref="Models.Result{T}"/>'s property names exactly so
/// a client parses one envelope whether the failure was expected or unexpected.
/// </summary>
/// <remarks>
/// <para>
/// A POCO rather than an anonymous object so the shape is declared once and cannot drift between
/// the three APIs. Property names are PascalCase and are kept that way by
/// <c>PropertyNamingPolicy = null</c> in each Program.cs; <see cref="EnumRespType"/> serialises
/// as its name via <c>JsonStringEnumConverter</c>.
/// </para>
/// <para>
/// <see cref="Errors"/> is populated only for a 400. It is <c>null</c> for a 500 by design - an
/// unexpected exception's field mapping would be guesswork, and the detail belongs in the log,
/// not the response.
/// </para>
/// </remarks>
public sealed class ErrorEnvelope
{
    /// <summary>Always <see cref="EnumRespType.Error"/> or <see cref="EnumRespType.SystemError"/>.</summary>
    public required EnumRespType RespType { get; init; }

    /// <summary>Message code from <c>Magizine.Shared.JsonResources.JsonResource</c>.</summary>
    public required string RespCode { get; init; }

    /// <summary>Human-readable summary. Never an exception message.</summary>
    public required string RespDesp { get; init; }

    /// <summary>Correlates the response with the server log entry for this failure.</summary>
    public required string TraceId { get; init; }

    /// <summary>Per-field problems, for a 400 only. <c>null</c> otherwise.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
