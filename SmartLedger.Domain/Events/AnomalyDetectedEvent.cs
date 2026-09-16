using SmartLedger.Domain.Common;

namespace SmartLedger.Domain.Events;

public sealed record AnomalyDetectedEvent(
    Guid InvoiceId,
    Guid TenantId,
    string Reason,
    decimal Amount,
    decimal ExpectedAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
