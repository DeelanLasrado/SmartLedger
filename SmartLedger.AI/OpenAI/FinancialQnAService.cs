using System.Text;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.AI.OpenAI;

public class FinancialQnAService(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IOpenAIService openAiService,
    IInvoiceRepository invoiceRepository) : IFinancialQnAService
{
    public async Task<string> AskAsync(Guid tenantId, string question, CancellationToken ct = default)
    {
        var queryEmbedding = await embeddingService.EmbedAsync(question, ct);
        var hits = await vectorStore.SearchAsync(tenantId, queryEmbedding, topK: 8, ct);

        var context = new StringBuilder();
        if (hits.Count > 0)
        {
            context.AppendLine("Retrieved invoice context:");
            foreach (var hit in hits)
                context.AppendLine($"- ({hit.Score:F3}) {hit.Summary}");
        }
        else
        {
            var recent = await invoiceRepository.GetByTenantAsync(tenantId, 0, 10, ct);
            context.AppendLine("Recent invoices (fallback):");
            foreach (var inv in recent)
                context.AppendLine($"- {inv.ToSummary()}");
        }

        var prompt = $"""
            Tenant financial Q&A.
            Question: {question}

            Context:
            {context}

            Answer using only the context when possible. If data is insufficient, say what is missing.
            """;

        return await openAiService.CompleteAsync(prompt, ct);
    }
}
