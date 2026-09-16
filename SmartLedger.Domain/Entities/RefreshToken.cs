using SmartLedger.Domain.Common;

namespace SmartLedger.Domain.Entities;

public class RefreshToken : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    private RefreshToken() { }

    public static RefreshToken Create(Guid tenantId, string userId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            TenantId = tenantId,
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        Touch();
    }
}
