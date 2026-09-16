using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(ApplicationDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Token == token, ct);

    public Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        db.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }
}
