namespace Magizine.Shared.Exceptions;

/// <summary>
/// Accumulates field-level validation problems so they can all be reported in one response,
/// then thrown as a single <see cref="ValidationException"/>.
/// </summary>
/// <remarks>
/// <example>
/// <code>
/// var errors = new ValidationErrorBuilder();
/// if (string.IsNullOrWhiteSpace(req.Title))
///     errors.Add(nameof(req.Title), "Title is required.");
/// if (req.ArticleCategoryId <= 0)
///     errors.Add(nameof(req.ArticleCategoryId), "A category must be selected.");
/// errors.ThrowIfInvalid();
/// </code>
/// </example>
/// <para>
/// Field names are matched case-insensitively, so adding "Title" and "title" is one entry with
/// two messages rather than two entries the client cannot distinguish. Repeated
/// <see cref="Add"/> calls for the same field append.
/// </para>
/// <para>
/// Prefer a leading dot for nested or indexed paths (<c>Author.Email</c>, <c>Items[0].Id</c>) so
/// a client can map the message back to a control without guessing.
/// </para>
/// </remarks>
public sealed class ValidationErrorBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when at least one problem has been recorded.</summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>Number of distinct fields with problems, not the number of messages.</summary>
    public int FieldCount => _errors.Count;

    /// <summary>Records a message against <paramref name="field"/>, appending if already present.</summary>
    public ValidationErrorBuilder Add(string field, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
        return this;
    }

    /// <summary>Records a message only when <paramref name="condition"/> is true.</summary>
    public ValidationErrorBuilder AddIf(bool condition, string field, string message)
        => condition ? Add(field, message) : this;

    /// <summary>Records <paramref name="message"/> when <paramref name="value"/> is null, empty, or whitespace.</summary>
    public ValidationErrorBuilder AddIfMissing(string? value, string field, string message)
        => AddIf(string.IsNullOrWhiteSpace(value), field, message);

    /// <summary>Snapshot of the accumulated problems, or <c>null</c> when there are none.</summary>
    public Dictionary<string, string[]>? ToDictionary()
        => _errors.Count == 0
            ? null
            : _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if anything was recorded. Call this once, after
    /// all checks, so the client receives every problem at once.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (ToDictionary() is { } errors)
        {
            throw new ValidationException(errors);
        }
    }
}
