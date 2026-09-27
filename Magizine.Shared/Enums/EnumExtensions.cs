using System.Runtime.CompilerServices;

namespace Magizine.Shared.Enums;

/// <summary>
/// Converts between the stringly-typed varchar status columns and the enums in this namespace.
/// </summary>
/// <remarks>
/// <para>
/// The six enum-backed columns (<c>Tbl_Article.Status</c>, <c>Tbl_RequestArticle.Status</c>,
/// <c>Tbl_Admin.Role</c>, <c>Tbl_Notification.Type</c>, <c>Tbl_Notification.RelatedEntityType</c>,
/// <c>Tbl_Ads.Placement</c>) are <c>varchar</c> with no <c>CHECK</c> constraint, because the
/// database is the source of truth and we deliberately make no schema change. The cost is that
/// nothing at the storage layer rejects a bad value - so this type is where that protection lives.
/// </para>
/// <para>
/// Member names ARE the stored values, so conversion is just <c>ToString</c> / <c>Enum.Parse</c>.
/// Reads use <see cref="ToEnum{TEnum}"/>, which throws on an unrecognised value rather than
/// silently defaulting - a typo in a hand-edited row should surface loudly, not become
/// <c>Draft</c>. Use <see cref="TryToEnum{TEnum}"/> only where a legacy or unrecognised value is
/// genuinely expected and you have a real fallback.
/// </para>
/// </remarks>
public static class EnumExtensions
{
    /// <summary>
    /// Renders <paramref name="value"/> as the exact string stored in the varchar column.
    /// </summary>
    public static string ToDbString<TEnum>(this TEnum value)
        where TEnum : struct, Enum
        => value.ToString() ?? throw new InvalidOperationException(
            $"Enum {typeof(TEnum).Name} produced a null name; it cannot be stored.");

    /// <summary>
    /// Parses a column value into <typeparamref name="TEnum"/>, throwing if it is not a known
    /// member. Use this for reads where an unexpected value is a bug or a data problem.
    /// </summary>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> is null, empty, or not a member of <typeparamref name="TEnum"/>.
    /// </exception>
    public static TEnum ToEnum<TEnum>(this string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(
                $"{parameterName} was null or empty; expected a {typeof(TEnum).Name} value.");
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw new FormatException(
                $"'{value}' is not a valid {typeof(TEnum).Name}. "
                + $"Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }

        return parsed;
    }

    /// <summary>
    /// Parses a column value into <typeparamref name="TEnum"/>, falling back to
    /// <paramref name="fallback"/> instead of throwing. For legacy rows only.
    /// </summary>
    public static TEnum ToEnumOrDefault<TEnum>(this string? value, TEnum fallback)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    /// <summary>
    /// Non-throwing parse, for filtering and query-building where an unknown value should simply
    /// match nothing rather than fail the whole request.
    /// </summary>
    public static bool TryToEnum<TEnum>(this string? value, out TEnum result)
        where TEnum : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out result);
}
