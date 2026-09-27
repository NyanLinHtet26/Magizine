using Magizine.DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace Magizine.DataBase;

/// <summary>
/// Hand-written behaviour layered on top of the scaffolded <see cref="AppDbContext"/>.
/// Lives outside EFContext/ so that re-running Doc\Rescaffold.ps1 never clobbers it.
/// </summary>
public class MagizineDbContext : AppDbContext
{
    // The scaffolded base constructor takes DbContextOptions<AppDbContext>, and
    // DbContextOptions<T> is not covariant, so rebuild the base options keyed by extension
    // type. That set carries the Npgsql provider and the resolved connection string.
    public MagizineDbContext(DbContextOptions<MagizineDbContext> options)
        : base(new DbContextOptions<AppDbContext>(
            options.Extensions.ToDictionary(extension => extension.GetType(), extension => extension)))
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tbl_AboutAndAppData is deliberately absent: it has no soft-delete columns.
        modelBuilder.Entity<TblAdmin>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblAuthor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblArticleCategory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblArticle>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblRequestArticle>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblContactMessage>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblNewsletter>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblNotification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TblAd>().HasQueryFilter(e => !e.IsDeleted);
    }
}
