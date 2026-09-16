using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Persistence.Repositories;

public class GstEntryRepository(ApplicationDbContext db) : IGstEntryRepository
{
    public async Task AddRangeAsync(IEnumerable<GstEntry> entries, CancellationToken ct = default) =>
        await db.GstEntries.AddRangeAsync(entries, ct);

    public async Task<IReadOnlyList<GstEntry>> GetByPeriodAsync(
        Guid tenantId, string period, CancellationToken ct = default) =>
        await db.GstEntries
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.Period == period)
            .ToListAsync(ct);
}
