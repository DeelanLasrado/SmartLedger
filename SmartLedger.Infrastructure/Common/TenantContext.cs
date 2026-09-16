namespace SmartLedger.Infrastructure.Common;

public sealed class TenantContext : ITenantContext
{
    public Guid CurrentTenantId { get; set; }
}
