namespace SmartLedger.Domain.ValueObjects;

public sealed record ExtractedInvoiceData
{
    public string VendorName { get; init; } = string.Empty;
    public string? VendorGstin { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal Cgst { get; init; }
    public decimal Sgst { get; init; }
    public decimal Igst { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateTime? InvoiceDate { get; init; }
    public string Category { get; init; } = "Uncategorized";
    public IReadOnlyList<ExtractedLineItem> LineItems { get; init; } = [];
}

public sealed record ExtractedLineItem(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount,
    decimal? GstRate);
