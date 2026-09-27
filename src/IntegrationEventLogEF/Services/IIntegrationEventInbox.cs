namespace eShop.IntegrationEventLogEF.Services;

public interface IIntegrationEventInbox
{
    Task<bool> TryEnlistAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task<bool> SaveEnlistedAsync(Func<Task> save);

    Task<bool> TrySaveAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid eventId, CancellationToken cancellationToken = default);
}
