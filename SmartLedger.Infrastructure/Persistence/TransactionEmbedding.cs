using SmartLedger.Domain.Common;

namespace SmartLedger.Infrastructure.Persistence;

public class TransactionEmbedding : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string EmbeddingJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
