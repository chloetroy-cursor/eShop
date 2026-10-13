using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF;
using eShop.IntegrationEventLogEF.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eShop.IntegrationEventLogEF.UnitTests;

[TestClass]
public class IntegrationEventInboxTests
{
    [TestMethod]
    public async Task Second_save_of_the_same_event_id_is_rejected()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<InboxContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new InboxContext(options);
        await context.Database.EnsureCreatedAsync();

        var integrationEvent = new ProbeIntegrationEvent();
        var inbox = new IntegrationEventInbox<InboxContext>(context);

        var first = await inbox.TrySaveAsync(integrationEvent);
        var second = await inbox.TrySaveAsync(integrationEvent);
        var stored = await context.Set<IntegrationEventInboxEntry>().CountAsync();

        Assert.IsTrue(first);
        Assert.IsFalse(second);
        Assert.AreEqual(1, stored);
    }

    [TestMethod]
    public async Task Concurrent_insert_of_the_same_event_id_hits_the_primary_key()
    {
        var connectionString = await Postgres.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<InboxContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var setup = new InboxContext(options);
        await setup.Database.EnsureCreatedAsync();

        await using var first = new InboxContext(options);
        await using var second = new InboxContext(options);
        var integrationEvent = new ProbeIntegrationEvent();
        first.Add(new IntegrationEventInboxEntry(integrationEvent));
        second.Add(new IntegrationEventInboxEntry(integrationEvent));
        await first.SaveChangesAsync();

        var duplicate = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => second.SaveChangesAsync());

        Assert.IsTrue(IntegrationEventInbox<InboxContext>.IsDuplicateKey(duplicate));
        Assert.AreEqual(1, await first.Set<IntegrationEventInboxEntry>().CountAsync());
    }

    [TestMethod]
    public void Unique_violation_on_another_table_is_not_an_inbox_duplicate()
    {
        Assert.IsFalse(IntegrationEventInbox<InboxContext>.IsDuplicateKey(
            Violation("ClientRequest", "PK_ClientRequest")));
        Assert.IsTrue(IntegrationEventInbox<InboxContext>.IsDuplicateKey(
            Violation("IntegrationEventInbox", "PK_IntegrationEventInbox")));
        Assert.IsTrue(IntegrationEventInbox<InboxContext>.IsDuplicateKey(
            Violation("Other", "PK_IntegrationEventInbox")));
    }

    static DbUpdateException Violation(string tableName, string constraintName)
    {
        var postgres = new PostgresException(
            "duplicate key",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            tableName: tableName,
            constraintName: constraintName);
        return new DbUpdateException("save failed", postgres);
    }

    sealed class InboxContext(DbContextOptions<InboxContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseIntegrationEventInbox();
        }
    }

    sealed record ProbeIntegrationEvent : IntegrationEvent;
}
