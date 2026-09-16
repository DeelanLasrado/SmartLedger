using System.Text.RegularExpressions;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartLedger.AI.Options;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.AI.DocumentIntelligence;

public class DocumentIntelligenceService(
    IBlobStorageService blobStorage,
    IOptions<AzureAiOptions> options,
    ILogger<DocumentIntelligenceService> logger) : IDocumentIntelligenceService
{
    private readonly AzureAiOptions _options = options.Value;

    public Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct = default) =>
        blobStorage.UploadAsync(file, fileName, ct);

    public async Task<ExtractedInvoiceData> ExtractInvoiceFieldsAsync(string blobUrl, CancellationToken ct = default)
    {
        if (!_options.HasFormRecognizer)
            return MockExtract(Path.GetFileName(blobUrl));

        return await ExtractWithFormRecognizerAsync(blobUrl, ct);
    }

    public async Task<ExtractedInvoiceData> ExtractInvoiceFieldsAsync(
        Stream file, string fileName, CancellationToken ct = default)
    {
        if (!_options.HasFormRecognizer)
        {
            logger.LogInformation("Form Recognizer credentials missing — using mock extraction for {File}", fileName);
            return MockExtract(fileName);
        }

        if (file.CanSeek)
            file.Position = 0;

        var client = new DocumentAnalysisClient(
            new Uri(_options.FormRecognizerEndpoint),
            new AzureKeyCredential(_options.FormRecognizerKey));

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed, "prebuilt-invoice", file, cancellationToken: ct);

        return MapInvoice(operation.Value, fileName);
    }

    private async Task<ExtractedInvoiceData> ExtractWithFormRecognizerAsync(string blobUrl, CancellationToken ct)
    {
        var client = new DocumentAnalysisClient(
            new Uri(_options.FormRecognizerEndpoint),
            new AzureKeyCredential(_options.FormRecognizerKey));

        var operation = await client.AnalyzeDocumentFromUriAsync(
            WaitUntil.Completed, "prebuilt-invoice", new Uri(blobUrl), cancellationToken: ct);

        return MapInvoice(operation.Value, Path.GetFileName(blobUrl));
    }

    private static ExtractedInvoiceData MapInvoice(AnalyzeResult result, string fileName)
    {
        var doc = result.Documents.FirstOrDefault();
        if (doc is null)
            return MockExtract(fileName);

        string? GetString(string field) =>
            doc.Fields.TryGetValue(field, out var f) ? f.Content : null;

        decimal GetAmount(string field)
        {
            if (doc.Fields.TryGetValue(field, out var f))
            {
                try
                {
                    var currency = f.Value.AsCurrency();
                    return (decimal)currency.Amount;
                }
                catch
                {
                    // fall through to string parse
                }
            }

            if (decimal.TryParse(GetString(field)?.Replace("₹", "").Replace(",", ""), out var parsed))
                return parsed;
            return 0m;
        }

        var vendor = GetString("VendorName") ?? "Unknown Vendor";
        var invoiceNumber = GetString("InvoiceId");
        DateTime? invoiceDate = null;
        if (doc.Fields.TryGetValue("InvoiceDate", out var dateField))
        {
            try
            {
                invoiceDate = dateField.Value.AsDate().DateTime;
            }
            catch
            {
                // ignore unparseable dates
            }
        }

        var total = GetAmount("InvoiceTotal");
        var taxable = total > 0 ? Math.Round(total / 1.18m, 2) : 0m;
        var tax = Math.Round(total - taxable, 2);

        return new ExtractedInvoiceData
        {
            VendorName = vendor,
            VendorGstin = ExtractGstin(GetString("VendorAddress") + " " + GetString("VendorTaxId")),
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow.Date,
            TotalAmount = total,
            TaxableAmount = taxable,
            Cgst = Math.Round(tax / 2, 2),
            Sgst = Math.Round(tax / 2, 2),
            Igst = 0,
            Category = InferCategory(vendor, fileName),
            LineItems =
            [
                new ExtractedLineItem("Extracted items", 1, taxable, taxable, 18)
            ]
        };
    }

    internal static ExtractedInvoiceData MockExtract(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        var (vendor, gstin, category, amount) = lower switch
        {
            var f when f.Contains("reliance") || f.Contains("retail") =>
                ("Reliance Retail Ltd", "27AABCR1234A1Z5", "Groceries", 12500.00m),
            var f when f.Contains("jio") || f.Contains("telecom") =>
                ("Reliance Jio Infocomm", "27AABCR5678B1Z9", "Telecom", 2499.00m),
            var f when f.Contains("amazon") || f.Contains("aws") =>
                ("Amazon Seller Services", "29AABCA1234C1Z2", "Office Supplies", 8750.50m),
            var f when f.Contains("flipkart") =>
                ("Flipkart Internet Pvt Ltd", "29AABCF4321D1Z8", "Inventory", 15200.00m),
            var f when f.Contains("electric") || f.Contains("bescom") =>
                ("BESCOM Electricity", "29AABCB1111E1Z3", "Utilities", 4200.00m),
            var f when f.Contains("fuel") || f.Contains("petrol") || f.Contains("hpcl") =>
                ("Hindustan Petroleum", "27AAACH1234F1Z6", "Fuel", 6800.00m),
            _ => ("Demo Kirana Wholesaler", "29AABCD1234G1Z7", "Inventory", 9999.00m)
        };

        // Heuristic: amount hint in filename like amt_15000
        var amtMatch = Regex.Match(fileName, @"amt[_-]?(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (amtMatch.Success && decimal.TryParse(amtMatch.Groups[1].Value, out var hinted))
            amount = hinted;

        var taxable = Math.Round(amount / 1.18m, 2);
        var halfTax = Math.Round((amount - taxable) / 2, 2);

        return new ExtractedInvoiceData
        {
            VendorName = vendor,
            VendorGstin = gstin,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Math.Abs(fileName.GetHashCode() % 10000):D4}",
            InvoiceDate = DateTime.UtcNow.Date.AddDays(-(Math.Abs(fileName.GetHashCode()) % 14)),
            TotalAmount = amount,
            TaxableAmount = taxable,
            Cgst = halfTax,
            Sgst = halfTax,
            Igst = 0,
            Category = category,
            LineItems =
            [
                new ExtractedLineItem($"{category} supplies", 1, taxable, taxable, 18)
            ]
        };
    }

    private static string? ExtractGstin(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = Regex.Match(text, @"[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}");
        return match.Success ? match.Value : null;
    }

    private static string InferCategory(string vendor, string fileName)
    {
        var text = $"{vendor} {fileName}".ToLowerInvariant();
        if (text.Contains("electric") || text.Contains("water")) return "Utilities";
        if (text.Contains("fuel") || text.Contains("petrol")) return "Fuel";
        if (text.Contains("jio") || text.Contains("airtel")) return "Telecom";
        if (text.Contains("amazon") || text.Contains("office")) return "Office Supplies";
        return "Inventory";
    }
}
