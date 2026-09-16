using System.Collections.Concurrent;
using System.Text.Json;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.AI.VectorStore;

public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, StoredVector>> _store = new();

    public Task UpsertAsync(
        Guid tenantId, Guid documentId, string summary, float[] embedding, CancellationToken ct = default)
    {
        var tenantStore = _store.GetOrAdd(tenantId, _ => new ConcurrentDictionary<Guid, StoredVector>());
        tenantStore[documentId] = new StoredVector(documentId, summary, embedding);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        Guid tenantId, float[] queryEmbedding, int topK = 10, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(tenantId, out var tenantStore) || tenantStore.IsEmpty)
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);

        var results = tenantStore.Values
            .Select(v => new VectorSearchResult(v.DocumentId, v.Summary, CosineSimilarity(queryEmbedding, v.Embedding)))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    internal static float CosineSimilarity(float[] a, float[] b)
    {
        var len = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom < 1e-9 ? 0f : (float)(dot / denom);
    }

    private sealed record StoredVector(Guid DocumentId, string Summary, float[] Embedding);

    public static string SerializeEmbedding(float[] embedding) => JsonSerializer.Serialize(embedding);
    public static float[] DeserializeEmbedding(string json) =>
        JsonSerializer.Deserialize<float[]>(json) ?? [];
}
