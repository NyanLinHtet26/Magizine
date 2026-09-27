using System.Diagnostics;
using Magizine.DataBase;
using Magizine.Shared.Enums;
using Magizine.Shared.Exceptions;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace MagizineAdmin.Api;

/// <summary>
/// Composition root. Every service and cross-cutting concern is registered here from one chain, so
/// Program.cs stays a readable summary of the pipeline rather than a wall of registrations.
/// </summary>
/// <remarks>
/// Follows the reference project's <c>AddModularService</c> shape, with three deliberate
/// departures, each for a security reason:
/// <list type="bullet">
/// <item>The signing key is validated at startup instead of being read as
/// <c>?? string.Empty</c>. An empty key does not fail loudly - it produces tokens anyone can
/// forge.</item>
/// <item>CORS uses an explicit origin allowlist that is empty by default, not the reference's
/// <c>AllowAnyOrigin()</c>.</item>
/// <item>401 and 403 responses are written as the same <see cref="ErrorEnvelope"/> as every other
/// failure, instead of the framework's empty-bodied challenge.</item>
/// </list>
/// </remarks>
public static class FeatureManager
{
    /// <summary>Display name used in the OpenAPI document.</summary>
    private const string ApiTitle = "Magizine Admin API";

    /// <summary>
    /// Registers everything the API needs. Call once from Program.cs.
    /// </summary>
    public static WebApplicationBuilder AddModularService(this WebApplicationBuilder builder)
        => builder
            .AddDatabaseServices()
            .AddCorsServices()
            .AddJwtServices()
            .AddSecurityServices();

    private static WebApplicationBuilder AddDatabaseServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddMagizineDatabase(builder.Configuration);
        return builder;
    }

    /// <summary>
    /// Restricts which browser origins may call this API. With an empty allowlist this registers a
    /// policy that permits nothing, which is the correct default while there is no frontend.
    /// </summary>
    private static WebApplicationBuilder AddCorsServices(this WebApplicationBuilder builder)
    {
        var appName = typeof(FeatureManager).Assembly.GetName().Name!;

        var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();

        // Throws on a wildcard or malformed entry, so a typo cannot quietly open the API to
        // every site on the internet.
        corsOptions.Validate(appName);

        builder.Services.AddSingleton(Options.Create(corsOptions));

        using var startupLoggerFactory = LoggerFactory.Create(logging => logging
            .AddConfiguration(builder.Configuration.GetSection("Logging"))
            .AddConsole());

        if (corsOptions.IsEmpty)
        {
            startupLoggerFactory
                .CreateLogger($"{appName}.Cors")
                .LogWarning(
                    "No '{Cors}:AllowedOrigins' configured, so browser calls from other origins are "
                    + "blocked. That is expected until a frontend exists; add origins to the {Section} "
                    + "config section when one does.",
                    CorsOptions.SectionName,
                    CorsOptions.SectionName);
        }

        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (corsOptions.IsEmpty)
            {
                // Deliberately add no origins: same-origin and non-browser callers still work.
                return;
            }

            policy
                .WithOrigins(corsOptions.AllowedOrigins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                // Tight on purpose. A custom request header needs adding here, and the preflight
                // failure that results is a clear signal rather than a silent open door.
                .WithHeaders("Authorization", "Content-Type", "Accept");
        }));

        return builder;
    }

    private static WebApplicationBuilder AddJwtServices(this WebApplicationBuilder builder)
    {
        var appName = typeof(FeatureManager).Assembly.GetName().Name!;

        // Bind and validate eagerly. JwtOptions.Validate throws with setup instructions if the
        // key is missing or too short, so a misconfigured deployment cannot start at all.
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"The '{JwtOptions.SectionName}' configuration section is missing from {appName}.");

        jwtOptions.Validate(appName);

        builder.Services.AddSingleton(Options.Create(jwtOptions));

        // The host does not exist yet, so the challenge handler needs its own logger. It is only
        // used for rejected-token warnings, which is worth one throwaway factory at startup.
        using var startupLoggerFactory = LoggerFactory.Create(logging => logging
            .AddConfiguration(builder.Configuration.GetSection("Logging"))
            .AddConsole());
        var logger = startupLoggerFactory.CreateLogger($"{appName}.Authentication");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Validation rules come from Shared so the Admin and Author APIs cannot diverge.
                options.TokenValidationParameters = jwtOptions.ToValidationParameters();

                // Keep raw RFC 7519 claim names, so a "role" claim arrives as "role" rather than
                // being silently rewritten to ClaimTypes.Role.
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    // HandleResponse is set on both, so the envelope is always the body. The
                    // framework's own challenge is an empty 401, which would break the one-shape
                    // contract clients rely on. Because we suppress it, WWW-Authenticate is
                    // re-added by hand in WriteAuthEnvelopeAsync - without that header a
                    // spec-compliant client has no way to know it should send a token at all.
                    OnChallenge = async context =>
                    {
                        if (context.AuthenticateFailure is { } failure)
                        {
                            // The reason matters server-side but must not reach the client.
                            logger.LogWarning(
                                failure,
                                "Bearer token rejected on {Method} {Path}",
                                context.HttpContext.Request.Method,
                                context.HttpContext.Request.Path);
                        }

                        context.HandleResponse();

                        await WriteAuthEnvelopeAsync(
                            context.HttpContext,
                            statusCode: StatusCodes.Status401Unauthorized,
                            respCode: JsonResource.Unauthorized,
                            respDesp: "Authentication is required to access this resource.",
                            challenge: true);
                    },

                    OnForbidden = context => WriteAuthEnvelopeAsync(
                        context.HttpContext,
                        statusCode: StatusCodes.Status403Forbidden,
                        respCode: JsonResource.Forbidden,
                        respDesp: "You do not have permission to perform this action.",
                        challenge: false)
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddOpenApi(options => options.AddDocumentTransformer(
            (document, _, _) =>
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title = ApiTitle;
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste the AccessToken from sign-in. No 'Bearer ' prefix needed."
                };
                return Task.CompletedTask;
            }));

        return builder;
    }

    /// <summary>
    /// Registers the stateless singletons. No DbContext here: it is scoped per request, so a
    /// singleton holding one would be a threading bug.
    /// </summary>
    private static WebApplicationBuilder AddSecurityServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<PasswordHasher>();
        builder.Services.AddSingleton<JwtTokenGenerator>();
        return builder;
    }

    /// <summary>
    /// Replaces the framework's empty-bodied 401/403 with the shared error envelope, so a client
    /// parses one shape for every failed request.
    /// </summary>
    /// <param name="challenge">
    /// True for a 401, which adds <c>WWW-Authenticate: Bearer</c>. Required because the default
    /// challenge that would normally set that header was suppressed.
    /// </param>
    private static Task WriteAuthEnvelopeAsync(
        HttpContext httpContext,
        int statusCode,
        string respCode,
        string respDesp,
        bool challenge)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        if (challenge && !httpContext.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate))
        {
            httpContext.Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer";
        }

        return httpContext.Response.WriteAsJsonAsync(
            new ErrorEnvelope
            {
                RespType = EnumRespType.Error,
                RespCode = respCode,
                RespDesp = respDesp,
                TraceId = traceId,
                Errors = null
            },
            httpContext.RequestAborted);
    }
}
