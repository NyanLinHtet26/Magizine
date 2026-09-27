namespace Magizine.Shared.Exceptions;

/// <summary>
/// Thrown when caller input is malformed, missing, or fails a format rule. Carries one or more
/// messages per field so the client can highlight the exact inputs at fault.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately does NOT extend <see cref="Models.Result{T}"/>-style expected outcomes. This is
/// reserved for input the server cannot even begin to act on, and the handlers map it to
/// HTTP 400. Business-rule violations ("that slug is taken", "you are not the author of this
/// article") are NOT validation errors - return those as
/// <c>Result&lt;T&gt;.Error(JsonResource.Something, message)</c> at HTTP 200 instead.
/// </para>
/// <para>
/// Lives in Shared with no ASP.NET Core dependency, mirroring the reference project's
/// <c>ValidationException</c> but usable by every API in the solution.
/// </para>
/// <para>
/// Accumulate with <see cref="ValidationErrorBuilder"/> rather than throwing on the first bad
/// field, so a client sees every problem in one round trip.
/// </para>
/// </remarks>
public sealed class ValidationException : Exception
{
    /// <summary>Field name (as the client sent it) to one or more messages about it.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("Validation failed.")
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException(
                "A ValidationException needs at least one error, otherwise use Result<T>.Error instead.",
                nameof(errors));
        }

        Errors = errors;
    }

    /// <summary>Single-message convenience for the common one-problem case.</summary>
    public ValidationException(string field, string message)
        : base("Validation failed.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [field] = [message]
        };
    }
}
