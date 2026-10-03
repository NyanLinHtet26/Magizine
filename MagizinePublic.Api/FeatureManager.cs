using Magizine.DataBase;
using Magizine.Shared.Security;
using MagizinePublic.Api.Features.ArticleCategory;
using MagizinePublic.Api.Features.Article;
using MagizinePublic.Api.Features.RequestArticle;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace MagizinePublic.Api;

/// <summary>
/// Composition root for the public API. Mirrors the Admin and Author composition roots so the
/// three stay recognisable, but with no JWT wiring: every endpoint here is unauthenticated.
/// </summary>
/// <remarks>
/// CORS is the one security control that matters here. This is the API a browser on the magazine
/// site will call, and it is the one with public write endpoints (pitch submission, contact form,
/// newsletter), so the origin allowlist is explicit and empty by default rather than
/// <c>AllowAnyOrigin()</c>.
/// </remarks>
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
            .AddOpenApiServices()
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
                .WithHeaders("Authorization", "Content-Type", "Accept");
        }));

        return builder;
    }

    private static WebApplicationBuilder AddOpenApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer(
            (document, _, _) =>
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title = ApiTitle;
                return Task.CompletedTask;
            }));

        return builder;
    }

    private static WebApplicationBuilder AddFeatureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ArticleCategoryService>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddScoped<RequestArticleService>();

        return builder;
    }
}
