using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLedger.AI.AnomalyDetection;
using SmartLedger.AI.CashFlow;
using SmartLedger.AI.DocumentIntelligence;
using SmartLedger.AI.OpenAI;
using SmartLedger.AI.Options;
using SmartLedger.AI.VectorStore;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.AI.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureAiOptions>(configuration.GetSection(AzureAiOptions.SectionName));

        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddScoped<IOpenAIService, OpenAIService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IFinancialQnAService, FinancialQnAService>();
        services.AddScoped<IAnomalyDetector, AnomalyDetector>();
        services.AddScoped<ICashFlowForecaster, CashFlowForecaster>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();

        return services;
    }
}
