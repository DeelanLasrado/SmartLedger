namespace SmartLedger.Infrastructure.Common;

public interface ITenantContext
{
    Guid CurrentTenantId { get; set; }
}
