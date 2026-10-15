using Npgsql;

namespace ProfessionalMatcher.Infra.Postgres;

/// <summary>Shared plumbing: every operation opens (and disposes) its own connection, as required behind PgBouncer.</summary>
internal abstract class PostgresRepository(Func<NpgsqlConnection> connectionFactory)
{
    protected async Task<int> Execute(string sql, Action<NpgsqlCommand> bind, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        bind(cmd);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    protected async Task<List<T>> Query<T>(
        string sql, Action<NpgsqlCommand> bind, Func<NpgsqlDataReader, T> map, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, connection);
        bind(cmd);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(map(reader));
        return rows;
    }

    protected async Task<T?> QuerySingle<T>(
        string sql, Action<NpgsqlCommand> bind, Func<NpgsqlDataReader, T> map, CancellationToken cancellationToken)
        where T : class =>
        (await Query(sql, bind, map, cancellationToken)).FirstOrDefault();
}
