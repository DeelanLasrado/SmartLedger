using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
