using eShop.Catalog.API.Infrastructure;
using eShop.Ordering.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Webhooks.API.Infrastructure;

namespace eShop.IntegrationEventLogEF.UnitTests;

[TestClass]
public class InboxMigrationTests
{
    [TestMethod]
    public async Task Catalog_migration_creates_the_inbox_table()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .Options;
        await using var context = new CatalogContext(options, new ConfigurationBuilder().Build());
        await context.Database.MigrateAsync();

        Assert.IsTrue(await TableExistsAsync(connectionString, "IntegrationEventInbox", schema: "public"));
    }

    [TestMethod]
    public async Task Ordering_migration_creates_the_inbox_table()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<OrderingContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new OrderingContext(options);
        await context.Database.MigrateAsync();

        Assert.IsTrue(await TableExistsAsync(connectionString, "IntegrationEventInbox", schema: "ordering"));
    }

    [TestMethod]
    public async Task Webhooks_migration_creates_the_inbox_table()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<WebhooksContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new WebhooksContext(options);
        await context.Database.MigrateAsync();

        Assert.IsTrue(await TableExistsAsync(connectionString, "IntegrationEventInbox", schema: "public"));
    }

    static async Task<bool> TableExistsAsync(string connectionString, string table, string schema)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @schema AND table_name = @table)
            """,
            connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
