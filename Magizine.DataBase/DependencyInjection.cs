using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Magizine.DataBase;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="MagizineDbContext"/> against Npgsql/PostgreSQL. The DbContext is
    /// the derived type so the hand-written soft-delete query filters are applied; the
    /// scaffolded AppDbContext supplies the mappings. Shared by every API in the solution so
    /// the connection-string key and provider wiring are defined in exactly one place.
    /// </summary>
    public static IServiceCollection AddMagizineDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var appName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
                          ?? "the API project";

            throw new InvalidOperationException(
                $"Connection string 'DefaultConnection' is not configured for {appName}. "
                + "Set it locally with: "
                + "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<value>\" "
                + "--project <path-to-csproj>");
        }

        services.AddDbContext<MagizineDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
