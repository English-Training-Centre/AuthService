using AuthService.src.Application.Interfaces;
using Dapper;
using Npgsql;
using Polly;
using Polly.Retry;

namespace AuthService.src.Infrastructure.Persistence;

public sealed class PostgresDB : IPostgresDB
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresDB> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public PostgresDB(
            NpgsqlDataSource dataSource,
        ILogger<PostgresDB> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _retryPolicy = Policy
            .Handle<NpgsqlException>(ex => ex.IsTransient)
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetry: (exception, timeSpan, attempt, _) =>
                    _logger.LogWarning(
                        exception,
                        "Attempt {Attempt} failed executing PostgreSQL command. Retrying in {Delay}ms.",
                        attempt,
                        timeSpan.TotalMilliseconds));
    }

    // ---------- Queries ----------

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

            var result = await conn.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    sql,
                    parameters,
                    cancellationToken: cancellationToken));

            return result;
        });
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            return await conn.ExecuteAsync(sql, parameters);
        });
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        var result = await ExecuteAsync(
            cmd => cmd.ExecuteScalarAsync(cancellationToken),
            sql,
            parameters ?? Array.Empty<NpgsqlParameter>());

        return result is DBNull or null
            ? default
            : (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteScalarAsync<int>("SELECT 1", cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ---------- Core execution ----------

    private async Task<T> ExecuteAsync<T>(
        Func<NpgsqlCommand, Task<T>> executeFunc,
        string sql,
        NpgsqlParameter[] parameters)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var cmd = _dataSource.CreateCommand(sql);

            if (parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            cmd.CommandTimeout = 30;

            _logger.LogDebug(
                "Executing SQL: {Sql} | Parameters: {@Parameters}",
                sql,
                parameters.Select(p => new { p.ParameterName, p.Value }));

            try
            {
                return await executeFunc(cmd);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error executing SQL command: {Sql}", sql);
                throw;
            }
        });
    }

    // ---------- Static infra helpers ----------

    public static string BuildConnectionStringFromEnvironment()
    {
        var host = GetRequired("DB_PTGR_HOST");
        var port = GetRequired("DB_PTGR_PORT");
        var database = "etc_db_auth_service";
        var username = GetRequired("DB_PTGR_USER");
        var password = GetRequired("DB_PTGR_PASS");

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Database = database,
            Username = username,
            Password = password,

            Pooling = true,
            MinPoolSize = 10,
            MaxPoolSize = 100,
            Timeout = 15,
            CommandTimeout = 30,
            SslMode = SslMode.Prefer
        };

        return csb.ConnectionString;
    }

    private static string GetRequired(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Environment variable '{name}' is required but missing.");

        return value;
    }
}