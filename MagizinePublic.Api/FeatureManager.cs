using System.Diagnostics;
using Magizine.DataBase;
using Magizine.Shared.Enums;
using Magizine.Shared.Exceptions;
using Magizine.Shared.JsonResources;
using Magizine.Shared.Security;
using MagizinePublic.Api.Features.Article;
using MagizinePublic.Api.Features.ArticleCategory;
using MagizinePublic.Api.Features.Auth;
using MagizinePublic.Api.Features.Bookmark;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

namespace MagizinePublic.Api;

/// <summary>
/// Composition root for the Public Reader API. Public reading endpoints remain unauthenticated
/// (<c>[AllowAnonymous]</c>), while Reader Auth, Profile, Bookmark, and Author Subscription
/// features use JWT + <c>HttpOnly</c> cookies (<c>reader_access_token</c> &amp; <c>reader_refresh_token</c>).
/// </summary>
public static class FeatureManager
{
    /// <summary>Display name used in the OpenAPI document.</summary>
    private const string ApiTitle = "Magizine Public API";

    /// <summary>
    /// Registers everything the API needs. Call once from Program.cs.
    /// </summary>
    public static WebApplicationBuilder AddModularService(this WebApplicationBuilder builder)
        => builder
            .AddDatabaseServices()
            .AddCorsServices()
            .AddJwtServices()
            .AddSecurityServices()
            .AddFeatureServices();

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
                .WithHeaders("Authorization", "Content-Type", "Accept")
                .AllowCredentials();
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

        using var startupLoggerFactory = LoggerFactory.Create(logging => logging
            .AddConfiguration(builder.Configuration.GetSection("Logging"))
            .AddConsole());
        var logger = startupLoggerFactory.CreateLogger($"{appName}.Authentication");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = jwtOptions.ToValidationParameters();
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Priority 1: Standard Authorization: Bearer <token> header.
                        // Priority 2: Fallback to the HttpOnly reader_access_token cookie for Browser SPAs.
                        if (string.IsNullOrEmpty(context.Request.Headers.Authorization) &&
                            context.Request.Cookies.TryGetValue(MagizineClaims.ReaderAccessTokenCookie, out var cookieToken) &&
                            !string.IsNullOrWhiteSpace(cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        var tokenType = context.Principal?.FindFirst(MagizineClaims.TokenType)?.Value;
                        if (!string.Equals(tokenType, MagizineClaims.AccessTokenType, StringComparison.Ordinal))
                        {
                            context.Fail("Invalid token purpose.");
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        if (context.AuthenticateFailure is { } failure)
                        {
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

    private static WebApplicationBuilder AddSecurityServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<PasswordHasher>();
        builder.Services.AddSingleton<JwtTokenGenerator>();
        return builder;
    }

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

    private static WebApplicationBuilder AddFeatureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMagizineFileStorage(builder.Configuration);
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<ArticleCategoryService>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddScoped<BookmarkService>();

        return builder;
    }
}
