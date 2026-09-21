using Npgsql;

namespace eShop.IntegrationEventLogEF.UnitTests;

static class Postgres
{
    const string AdminConnectionString = "Host=127.0.0.1;Port=55432;Username=postgres;Password=pass;Database=postgres";

    public static async Task<string> CreateDatabaseAsync()
    {
        var name = "inbox_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
        await command.ExecuteNonQueryAsync();
        return $"Host=127.0.0.1;Port=55432;Username=postgres;Password=pass;Database={name}";
    }
}
