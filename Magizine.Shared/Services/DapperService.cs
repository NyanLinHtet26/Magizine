using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Magizine.Shared.Services;

/// <summary>
/// A generic Dapper wrapper for high-performance reading via PostgreSQL Stored Procedures / Functions.
/// Used for migrating heavy GET requests away from EF Core to raw SQL.
/// </summary>
public class DapperService
{
    private readonly string _connectionString;
    private readonly ILogger<DapperService> _logger;

    public DapperService() { } // Empty constructor for mocking

    public DapperService(IConfiguration configuration, ILogger<DapperService> logger)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }
        
        _connectionString = connStr;
        _logger = logger;
    }

    /// <summary>
    /// Executes a stored procedure/function and returns a single item.
    /// </summary>
    public virtual async Task<T?> GetFirstOrDefaultAsync<T>(string query, object? parameters = null)
    {
        try
        {
            await using var db = new NpgsqlConnection(_connectionString);
            return await db.QueryFirstOrDefaultAsync<T>(
                query, 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper Error in GetFirstOrDefaultAsync for {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure/function and returns a list of items.
    /// </summary>
    public virtual async Task<List<T>> GetListAsync<T>(string query, object? parameters = null)
    {
        try
        {
            await using var db = new NpgsqlConnection(_connectionString);
            var result = await db.QueryAsync<T>(
                query, 
                parameters, 
                commandType: CommandType.StoredProcedure);
                
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper Error in GetListAsync for {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure/function that returns a total count and a list, mapping it to a PagedResult.
    /// The SQL function MUST return "TotalCount" as the very first column, followed by the entity columns.
    /// </summary>
    public virtual async Task<Magizine.Shared.Models.Paging.PagedResult<T>> GetPagedListAsync<T>(
        string query, 
        object? parameters, 
        string splitOn, 
        Magizine.Shared.Models.Paging.PageRequest pageReq)
    {
        try
        {
            long totalCount = 0;
            await using var db = new NpgsqlConnection(_connectionString);
            
            var result = await db.QueryAsync<long, T, T>(
                query, 
                (count, item) => 
                {
                    totalCount = count; // Every row has the same count, we just grab it
                    return item;
                },
                parameters, 
                splitOn: splitOn,
                commandType: CommandType.StoredProcedure);
                
            return Magizine.Shared.Models.Paging.PagedResult<T>.Create(result.ToList(), totalCount, pageReq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper Error in GetPagedListAsync for {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure/function that returns multiple result sets (e.g. PageData + ListData).
    /// </summary>
    public virtual async Task<(TReturn1?, List<TReturn2>)> GetMultipleListAsync<TReturn1, TReturn2>(string query, object? parameters = null)
    {
        try
        {
            await using var db = new NpgsqlConnection(_connectionString);
            await using var multi = await db.QueryMultipleAsync(
                query, 
                parameters, 
                commandType: CommandType.StoredProcedure);
                
            var return1 = await multi.ReadFirstOrDefaultAsync<TReturn1>();
            var return2 = (await multi.ReadAsync<TReturn2>()).ToList();
            
            return (return1, return2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper Error in GetMultipleListAsync for {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Executes a stored procedure/function without returning a result set (e.g. bulk update).
    /// </summary>
    public virtual async Task<int> ExecuteAsync(string query, object? parameters = null)
    {
        try
        {
            await using var db = new NpgsqlConnection(_connectionString);
            return await db.ExecuteAsync(
                query, 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper Error in ExecuteAsync for {Query}", query);
            throw;
        }
    }
}
