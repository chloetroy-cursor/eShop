using Npgsql;

namespace eShop.IntegrationEventLogEF.Services;

public class IntegrationEventInbox<TContext> : IIntegrationEventInbox
    where TContext : DbContext
{
    private readonly TContext _context;

    public IntegrationEventInbox(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> TryEnlistAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (_context.Set<IntegrationEventInboxEntry>().Local.Any(entry => entry.EventId == integrationEvent.Id))
        {
            return false;
        }

        var exists = await _context.Set<IntegrationEventInboxEntry>()
            .AsNoTracking()
            .AnyAsync(entry => entry.EventId == integrationEvent.Id, cancellationToken);

        if (exists)
        {
            return false;
        }

        _context.Set<IntegrationEventInboxEntry>().Add(new IntegrationEventInboxEntry(integrationEvent));
        return true;
    }

    public async Task<bool> SaveEnlistedAsync(Func<Task> save)
    {
        ArgumentNullException.ThrowIfNull(save);

        try
        {
            await save();
            return true;
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            DetachAddedInboxEntries();
            return false;
        }
    }

    public async Task<bool> TrySaveAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (!await TryEnlistAsync(integrationEvent, cancellationToken))
        {
            return false;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            DetachAddedInboxEntries();
            return false;
        }
    }

    public async Task RemoveAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<IntegrationEventInboxEntry>()
            .SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken);

        if (entry is null)
        {
            return;
        }

        _context.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public static bool IsDuplicateKey(DbUpdateException exception)
    {
        for (Exception current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }

            if (current.GetType().Name == "SqliteException"
                && current.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void DetachAddedInboxEntries()
    {
        foreach (var entry in _context.ChangeTracker.Entries<IntegrationEventInboxEntry>()
                     .Where(item => item.State == EntityState.Added)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
