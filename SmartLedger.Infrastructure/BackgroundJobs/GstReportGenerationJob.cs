using Microsoft.Extensions.Logging;

namespace SmartLedger.Infrastructure.BackgroundJobs;

public class GstReportGenerationJob(ILogger<GstReportGenerationJob> logger)
{
    public Task ExecuteAsync(Guid tenantId, string period)
    {
        logger.LogInformation(
            "GST report generation job queued for tenant {TenantId}, period {Period}. (Stub — wire GSTR export here.)",
            tenantId, period);
        return Task.CompletedTask;
    }
}
