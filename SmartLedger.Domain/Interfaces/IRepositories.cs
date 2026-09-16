using SmartLedger.Domain.Entities;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByTenantAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetRecentByCategoryAsync(Guid tenantId, string category, int months, CancellationToken ct = default);
    Task<int> CountByTenantMonthAsync(Guid tenantId, int year, int month, CancellationToken ct = default);
}

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
}

public interface IGstEntryRepository
{
    Task AddRangeAsync(IEnumerable<GstEntry> entries, CancellationToken ct = default);
    Task<IReadOnlyList<GstEntry>> GetByPeriodAsync(Guid tenantId, string period, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IDocumentIntelligenceService
{
    Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct = default);
    Task<ExtractedInvoiceData> ExtractInvoiceFieldsAsync(string blobUrl, CancellationToken ct = default);
    Task<ExtractedInvoiceData> ExtractInvoiceFieldsAsync(Stream file, string fileName, CancellationToken ct = default);
}

public interface IAnomalyDetector
{
    Task<AnomalyResult> CheckAsync(Invoice invoice, CancellationToken ct = default);
}

public sealed record AnomalyResult(bool IsAnomaly, string Reason, decimal ExpectedAmount);

public interface IFinancialQnAService
{
    Task<string> AskAsync(Guid tenantId, string question, CancellationToken ct = default);
}

public interface ICashFlowForecaster
{
    Task<CashFlowForecastResult> ForecastAsync(Guid tenantId, int horizonDays, CancellationToken ct = default);
}

public sealed record CashFlowForecastResult(
    int HorizonDays,
    IReadOnlyList<CashFlowPoint> Points,
    decimal ConfidenceLower,
    decimal ConfidenceUpper);

public sealed record CashFlowPoint(DateTime Date, decimal PredictedBalance, decimal LowerBound, decimal UpperBound);

public interface IVectorStore
{
    Task UpsertAsync(Guid tenantId, Guid documentId, string summary, float[] embedding, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(Guid tenantId, float[] queryEmbedding, int topK = 10, CancellationToken ct = default);
}

public sealed record VectorSearchResult(Guid DocumentId, string Summary, float Score);

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}

public interface IOpenAIService
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
}

public interface IGstReconciliationService
{
    Task<GstReconciliationResult> ReconcileAsync(Guid tenantId, string period, CancellationToken ct = default);
}

public sealed record GstReconciliationResult(
    string Period,
    int MatchedCount,
    int MismatchCount,
    decimal NetTaxLiability,
    IReadOnlyList<GstMismatch> Mismatches);

public sealed record GstMismatch(
    string InvoiceNumber,
    string? CounterpartyGstin,
    decimal Gstr2AAmount,
    decimal Gstr3BAmount,
    string Reason);

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public interface IAlertService
{
    Task SendAnomalyAlertAsync(Guid tenantId, string email, string message, CancellationToken ct = default);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct = default);
}
