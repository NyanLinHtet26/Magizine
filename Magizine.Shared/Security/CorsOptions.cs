namespace Magizine.Shared.Security;

/// <summary>
/// Browser same-origin policy settings, bound from the <c>Cors</c> configuration section.
/// </summary>
/// <remarks>
/// <para>
/// The allowlist is <b>empty by default and that is the safe state</b>: with no origins listed the
/// CORS policy permits nothing, so a browser on another origin is blocked until someone names the
/// origins deliberately. A wildcard would be the opposite - it would hand every site on the
/// internet permission to call these APIs from a logged-in user's browser - so
/// <see cref="Validate"/> refuses one outright rather than trusting configuration.
/// </para>
/// <para>
/// Credentials are deliberately not enabled. Authentication is a bearer token in the
/// <c>Authorization</c> header, which a browser attaches only to requests it has been told to make,
/// so cookies are not involved and <c>AllowCredentials</c> is unnecessary. Leaving it off also
/// removes the combination that browsers forbid and that leaks authenticated responses: wildcard
/// origin plus credentials. If auth ever moves to cookies, revisit this and keep an explicit
/// origin list.
/// </para>
/// <para>
/// Set with a plain config array, or per environment without touching the file:
/// <c>Cors__AllowedOrigins__0=https://app.example.com</c>.
/// </para>
/// </remarks>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Exact scheme/host/port combinations permitted to call these APIs from a browser. No
    /// wildcards, no trailing slashes - an origin is a scheme, host and port only.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>True when nothing is allowed, i.e. only same-origin and non-browser callers work.</summary>
    public bool IsEmpty => AllowedOrigins.Length == 0;

    /// <summary>
    /// Throws if any entry is blank, malformed, or a wildcard. Called at startup so a bad entry is
    /// a deployment failure rather than a silently over-permissive API.
    /// </summary>
    public CorsOptions Validate(string appName)
    {
        var problems = new List<string>();

        foreach (var origin in AllowedOrigins)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                problems.Add($"'{SectionName}:AllowedOrigins' contains a blank entry.");
                continue;
            }

            if (origin.Contains('*', StringComparison.Ordinal))
            {
                problems.Add(
                    $"'{origin}' uses a wildcard. Name exact origins instead: "
                    + "a wildcard lets any website on the internet call this API from a user's browser.");
                continue;
            }

            if (origin.EndsWith('/'))
            {
                problems.Add($"'{origin}' has a trailing slash. An origin is scheme, host and port only.");
                continue;
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                problems.Add($"'{origin}' is not an absolute http(s) origin, for example https://app.example.com.");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"{appName} cannot start with an invalid '{SectionName}' configuration:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, problems.Select(problem => "  - " + problem)));
        }

        return this;
    }
}
