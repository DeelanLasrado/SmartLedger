namespace SmartLedger.AI.Options;

public sealed class AzureAiOptions
{
    public const string SectionName = "Azure";

    public string FormRecognizerEndpoint { get; set; } = string.Empty;
    public string FormRecognizerKey { get; set; } = string.Empty;
    public string OpenAIEndpoint { get; set; } = string.Empty;
    public string OpenAIKey { get; set; } = string.Empty;
    public string OpenAIDeployment { get; set; } = "gpt-4o";
    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
    public string BlobConnectionString { get; set; } = string.Empty;
    public string BlobContainer { get; set; } = "invoices";

    public bool HasFormRecognizer =>
        !string.IsNullOrWhiteSpace(FormRecognizerEndpoint) && !string.IsNullOrWhiteSpace(FormRecognizerKey);

    public bool HasOpenAI =>
        !string.IsNullOrWhiteSpace(OpenAIEndpoint) && !string.IsNullOrWhiteSpace(OpenAIKey);
}
