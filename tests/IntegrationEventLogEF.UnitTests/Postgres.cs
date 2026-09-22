using System.Threading;
using Npgsql;
using Testcontainers.PostgreSql;

namespace eShop.IntegrationEventLogEF.UnitTests;

static class Postgres
{
    static readonly PostgreSqlContainer Container = new PostgreSqlBuilder("ankane/pgvector")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("pass")
        .Build();

    static readonly SemaphoreSlim Gate = new(1, 1);
    static bool _started;

    public static async Task<string> CreateDatabaseAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (!_started)
            {
                await Container.StartAsync();
                _started = true;
            }
        }
        finally
        {
            Gate.Release();
        }

        var name = "inbox_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(Container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"""CREATE DATABASE "{name}" """, connection);
        await command.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(Container.GetConnectionString())
        {
            Database = name
        };
        return builder.ConnectionString;
    }
}
