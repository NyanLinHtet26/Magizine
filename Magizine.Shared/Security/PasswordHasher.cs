using System.Diagnostics.CodeAnalysis;

namespace Magizine.Shared.Security;

/// <summary>
/// BCrypt password hashing. The only place in the solution that may touch a raw password or a
/// stored hash.
/// </summary>
/// <remarks>
/// <para>
/// Chosen over SHA-256 family hashes because those are designed to be fast, which is exactly
/// wrong for passwords: a fast hash lets an attacker try billions of guesses per second against
/// a stolen database. BCrypt is deliberately slow and its cost is tunable, so the work factor can
/// be raised later without invalidating existing hashes.
/// </para>
/// <para>
/// Neither the password nor the hash may ever be logged, returned in a response model, or placed
/// in a URL. Hashes stay in <c>Tbl_Admin.PasswordHash</c> / <c>Tbl_Author.PasswordHash</c> and
/// must never be projected into a <c>*ResModel</c>.
/// </para>
/// <para>
/// Every method is thread-safe and stateless; register this as a singleton.
/// </para>
/// </remarks>
public sealed class PasswordHasher
{
    /// <summary>
    /// Default cost. 12 is roughly 250ms on current server hardware - slow enough to hurt an
    /// offline attacker, fast enough for an interactive login.
    /// </summary>
    public const int DefaultWorkFactor = 12;

    /// <summary>
    /// BCrypt ignores everything past 72 bytes of input. Silently truncating means two different
    /// long passwords would both authenticate, so <see cref="Hash"/> refuses instead and callers
    /// should reject over-long passwords as a validation error (HTTP 400) before reaching here.
    /// </summary>
    public const int MaxPasswordBytes = 72;

    /// <summary>Lowest cost accepted, to keep a misconfigured value from destroying security.</summary>
    public const int MinWorkFactor = 10;

    /// <summary>Highest cost accepted. Past ~15 an attacker-facing login becomes unusable.</summary>
    public const int MaxWorkFactor = 20;

    /// <summary>
    /// A BCrypt hash is always exactly 60 characters: <c>$2</c> + variant + <c>$</c> +
    /// two-digit cost + <c>$</c> + 22 salt characters + 31 hash characters.
    /// </summary>
    private const int BcryptHashLength = 60;

    private readonly int _workFactor;

    public PasswordHasher(int workFactor = DefaultWorkFactor)
    {
        if (workFactor is < MinWorkFactor or > MaxWorkFactor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workFactor),
                workFactor,
                $"BCrypt work factor must be between {MinWorkFactor} and {MaxWorkFactor}.");
        }

        _workFactor = workFactor;
    }

    /// <summary>
    /// Hashes a password for storage. The result embeds the salt and cost, so no separate salt
    /// column is needed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The password is empty or longer than <see cref="MaxPasswordBytes"/> bytes. Both are
    /// validation failures and should be reported as HTTP 400, not as an exception.
    /// </exception>
    public string Hash(string password)
    {
        Validate(password, nameof(password));
        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    /// <summary>
    /// Checks a password against a stored hash. Returns false rather than throwing for a missing
    /// or corrupt hash, so a damaged row denies access instead of surfacing as a 500.
    /// </summary>
    public bool Verify(string password, string? hash)
    {
        if (string.IsNullOrEmpty(password) || !IsWellFormedBcryptHash(hash))
        {
            return false;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
        {
            // Cannot be a match, and feeding it to BCrypt would silently compare a truncated prefix.
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
        {
            // The shape check above catches the cases that actually occur (truncated or plaintext
            // values in a column). This backstop is for anything the library rejects with a
            // different exception - a guard, not the primary defence, because a thrown exception
            // here would become a 500 on the login endpoint.
            return false;
        }
    }

    /// <summary>
    /// True when <paramref name="hash"/> was made with a lower cost than the current default, so
    /// the caller can transparently re-hash on the next successful login.
    /// </summary>
    public bool NeedsRehash(string? hash)
    {
        if (!IsWellFormedBcryptHash(hash))
        {
            return true;
        }

        try
        {
            return BCrypt.Net.BCrypt.PasswordNeedsRehash(hash!, _workFactor);
        }
        catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
        {
            return true;
        }
    }

    /// <summary>
    /// Cheap structural check that <paramref name="hash"/> could be a BCrypt hash at all.
    /// </summary>
    /// <remarks>
    /// Needed because the library is not consistent about how it rejects bad input: a truncated
    /// value such as <c>$2a$12$abc</c> raises <see cref="ArgumentOutOfRangeException"/> from
    /// substring arithmetic, not <c>SaltParseException</c>. Validating the shape first keeps a
    /// corrupt database row a failed login rather than an unhandled exception. Do not relax this
    /// to a try/catch around the library call alone.
    /// </remarks>
    private static bool IsWellFormedBcryptHash([NotNullWhen(true)] string? hash)
        => !string.IsNullOrWhiteSpace(hash)
           && hash.Length == BcryptHashLength
           && hash[0] == '$'
           && hash[1] == '2';

    private static void Validate(string password, string parameterName)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("A password is required.", parameterName);
        }

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(password);

        if (byteCount > MaxPasswordBytes)
        {
            throw new ArgumentException(
                $"A password may not exceed {MaxPasswordBytes} bytes; this one is {byteCount}. "
                + "BCrypt would silently truncate it, so it is rejected instead.",
                parameterName);
        }
    }
}
