using System.ComponentModel.DataAnnotations;

namespace eShop.IntegrationEventLogEF;

public class IntegrationEventInboxEntry
{
    private IntegrationEventInboxEntry() { }

    public IntegrationEventInboxEntry(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        EventId = integrationEvent.Id;
        EventTypeName = integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name;
        CreationTime = integrationEvent.CreationDate;
        ConsumedTime = DateTime.UtcNow;
    }

    public Guid EventId { get; private set; }

    [Required]
    public string EventTypeName { get; private set; }

    public DateTime CreationTime { get; private set; }

    public DateTime ConsumedTime { get; private set; }
}
