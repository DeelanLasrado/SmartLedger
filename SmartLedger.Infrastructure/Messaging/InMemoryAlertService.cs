using Microsoft.Extensions.Logging;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Messaging;

public class InMemoryAlertService(ILogger<InMemoryAlertService> logger) : IAlertService
{
    public Task SendAnomalyAlertAsync(Guid tenantId, string email, string message, CancellationToken ct = default)
    {
        logger.LogWarning(
            "ANOMALY ALERT | Tenant={TenantId} | To={Email} | {Message}",
            tenantId, email, message);
        return Task.CompletedTask;
    }
}
