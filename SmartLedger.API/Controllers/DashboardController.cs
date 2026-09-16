using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.Extensions;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController(
    IInvoiceRepository invoiceRepository,
    ITenantRepository tenantRepository) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<object>> Summary(CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct);
        var invoices = await invoiceRepository.GetByTenantAsync(tenantId, 0, 500, ct);

        var now = DateTime.UtcNow;
        var monthTotal = invoices
            .Where(i => i.InvoiceDate is { } d && d.Year == now.Year && d.Month == now.Month)
            .Sum(i => i.TotalAmount);

        return Ok(new
        {
            businessName = tenant?.BusinessName,
            tier = tenant?.Tier.ToString(),
            invoicesUsedThisMonth = tenant?.InvoicesUsedThisMonth ?? 0,
            monthlyInvoiceLimit = tenant?.MonthlyInvoiceLimit,
            invoiceCount = invoices.Count,
            monthSpend = monthTotal,
            anomalyCount = invoices.Count(i => i.IsAnomaly),
            categoryBreakdown = invoices
                .GroupBy(i => i.Category)
                .Select(g => new { category = g.Key, total = g.Sum(x => x.TotalAmount), count = g.Count() })
                .OrderByDescending(x => x.total)
        });
    }
}
