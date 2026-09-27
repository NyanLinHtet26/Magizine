using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Magizine.Shared.Models.Paging;

namespace Magizine.DataBase.Paging;

/// <summary>
/// EF Core paging extension that keeps the dependency out of Magizine.Shared.
/// </summary>
/// <remarks>
/// <para>
/// The extension performs <c>CountAsync()</c> on the unpaged query, then applies
/// <c>Skip</c>/<c>Take</c> and materializes the projected items. The projection
/// expression is required so we only SELECT the columns the DTO needs.
/// </para>
/// <para>
/// Stable ordering is the caller's responsibility - pass an <c>OrderBy</c> /
/// <c>ThenBy</c> chain before calling this.
/// </para>
/// </remarks>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// Projects and pages an <see cref="IQueryable{TSource}"/> to a <see cref="PagedResult{TResult}"/>.
    /// </summary>
    /// <typeparam name="TSource">Entity type from the DbSet.</typeparam>
    /// <typeparam name="TResult">DTO type the caller wants to return.</typeparam>
    /// <param name="query">
    /// The query <b>already ordered</b> (call <c>OrderBy</c>/<c>ThenBy</c> first).
    /// The query must NOT already have Skip/Take applied.
    /// </param>
    /// <param name="selector">Projection from entity to DTO.</param>
    /// <param name="request">Normalized paging parameters (page, page size).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PagedResult{TResult}"/> with items and metadata.</returns>
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TResult>> selector,
        PageRequest request,
        CancellationToken cancellationToken = default)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(request);

        var totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .Skip(request.Offset)
            .Take(request.PageSize)
            .Select(selector)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return PagedResult<TResult>.Create(items, totalCount, request);
    }
}