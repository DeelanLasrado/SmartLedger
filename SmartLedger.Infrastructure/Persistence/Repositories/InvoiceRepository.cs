using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(ApplicationDbContext db) : IInvoiceRepository
{
    public async Task AddAsync(Invoice invoice, CancellationToken ct = default) =>
        await db.Invoices.AddAsync(invoice, ct);

    public Task<Invoice?> GetByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default) =>
        db.Invoices
            .Include(i => i.LineItems)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == invoiceId, ct);

    public async Task<IReadOnlyList<Invoice>> GetByTenantAsync(
        Guid tenantId, int skip = 0, int take = 50, CancellationToken ct = default) =>
        await db.Invoices
            .Include(i => i.LineItems)
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.InvoiceDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetRecentByCategoryAsync(
        Guid tenantId, string category, int months, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Date.AddMonths(-months);
        return await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId
                        && i.Category == category
                        && i.InvoiceDate >= since)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(ct);
    }

    public Task<int> CountByTenantMonthAsync(Guid tenantId, int year, int month, CancellationToken ct = default) =>
        db.Invoices
            .IgnoreQueryFilters()
            .CountAsync(i => i.TenantId == tenantId
                             && i.InvoiceDate.HasValue
                             && i.InvoiceDate.Value.Year == year
                             && i.InvoiceDate.Value.Month == month, ct);
}
