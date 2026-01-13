using Npgsql;

namespace AuthService.src.Application.Interfaces;

public interface IPostgresDB
{
    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters);

    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<NpgsqlConnection, NpgsqlTransaction, Task<TResult>> action,
        CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(
        CancellationToken cancellationToken = default);
}