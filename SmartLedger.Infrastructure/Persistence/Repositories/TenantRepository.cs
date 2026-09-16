using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Persistence.Repositories;

public class TenantRepository(ApplicationDbContext db) : ITenantRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken ct = default) =>
        await db.Tenants.AddAsync(tenant, ct);

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);

    public Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Update(tenant);
        return Task.CompletedTask;
    }
}
