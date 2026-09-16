using SmartLedger.Domain.Common;

namespace SmartLedger.Domain.Events;

public sealed record InvoiceParsedEvent(
    Guid InvoiceId,
    Guid TenantId,
    string VendorName,
    decimal TotalAmount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
