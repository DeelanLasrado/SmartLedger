using System.ClientModel;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;
using SmartLedger.AI.Options;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.AI.OpenAI;

public class OpenAIService(
    IOptions<AzureAiOptions> options,
    ILogger<OpenAIService> logger) : IOpenAIService
{
    private readonly AzureAiOptions _options = options.Value;

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        if (!_options.HasOpenAI)
        {
            logger.LogInformation("OpenAI key missing — using keyword mock completion.");
            return MockComplete(prompt);
        }

        var client = new AzureOpenAIClient(
            new Uri(_options.OpenAIEndpoint),
            new ApiKeyCredential(_options.OpenAIKey));

        var chat = client.GetChatClient(_options.OpenAIDeployment);
        var result = await chat.CompleteChatAsync(
            [
                new SystemChatMessage("You are SmartLedger, a financial assistant for Indian SMBs. Be concise and cite figures from context."),
                new UserChatMessage(prompt)
            ],
            cancellationToken: ct);

        return result.Value.Content[0].Text;
    }

    internal static string MockComplete(string prompt)
    {
        var lower = prompt.ToLowerInvariant();
        if (lower.Contains("gst") || lower.Contains("tax"))
            return "Based on your recent invoices, estimated GST outflow is around 18% of taxable purchases. Review GSTR-2A mismatches in the GST reconcile endpoint for exact liability.";
        if (lower.Contains("cash") || lower.Contains("forecast") || lower.Contains("runway"))
            return "Cash-flow outlook: average daily spend from invoices suggests a stable burn. Use /api/cashflow/forecast?horizonDays=30 for a projected balance curve.";
        if (lower.Contains("anomal") || lower.Contains("unusual") || lower.Contains("spike"))
            return "Anomaly checks flag invoices above 3× the category historical average. Review invoices with IsAnomaly=true on the dashboard.";
        if (lower.Contains("top") || lower.Contains("vendor") || lower.Contains("spend"))
            return "Top spend appears concentrated with wholesale/inventory vendors. Upload more invoices to refine vendor ranking via RAG context.";
        return "I reviewed the retrieved invoice summaries in context. Ask about GST liability, cash-flow forecast, anomalies, or top vendors for a more specific answer.";
    }
}

public class EmbeddingService(
    IOptions<AzureAiOptions> options,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    private readonly AzureAiOptions _options = options.Value;
    private const int Dimensions = 384;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!_options.HasOpenAI)
        {
            logger.LogDebug("Using hash-based mock embeddings.");
            return MockEmbed(text);
        }

        var client = new AzureOpenAIClient(
            new Uri(_options.OpenAIEndpoint),
            new ApiKeyCredential(_options.OpenAIKey));

        var embeddingClient = client.GetEmbeddingClient(_options.EmbeddingDeployment);
        var result = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray();
    }

    internal static float[] MockEmbed(string text)
    {
        var vector = new float[Dimensions];
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToLowerInvariant()));
        var rng = new Random(BitConverter.ToInt32(hash, 0));
        for (var i = 0; i < Dimensions; i++)
            vector[i] = (float)(rng.NextDouble() * 2 - 1);

        // Mix in token signals for better keyword similarity in demos
        foreach (var token in text.Split([' ', '|', ',', ':'], StringSplitOptions.RemoveEmptyEntries))
        {
            var th = token.ToLowerInvariant().GetHashCode();
            var idx = Math.Abs(th) % Dimensions;
            vector[idx] += 0.35f;
        }

        Normalize(vector);
        return vector;
    }

    private static void Normalize(float[] v)
    {
        double sum = 0;
        foreach (var x in v) sum += x * x;
        var norm = Math.Sqrt(sum);
        if (norm < 1e-9) return;
        for (var i = 0; i < v.Length; i++)
            v[i] = (float)(v[i] / norm);
    }
}
