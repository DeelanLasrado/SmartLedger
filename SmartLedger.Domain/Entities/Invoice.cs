using SmartLedger.Domain.Common;
using SmartLedger.Domain.Enums;
using SmartLedger.Domain.Events;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Domain.Entities;

public class Invoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;
    public string? VendorGstin { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public DateTime? InvoiceDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal Cgst { get; private set; }
    public decimal Sgst { get; private set; }
    public decimal Igst { get; private set; }
    public string Category { get; private set; } = "Uncategorized";
    public string? BlobUrl { get; private set; }
    public string? OriginalFileName { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Pending;
    public bool IsAnomaly { get; private set; }
    public string? AnomalyReason { get; private set; }

    private readonly List<InvoiceLineItem> _lineItems = [];
    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    private Invoice() { }

    public static Invoice Create(Guid tenantId, ExtractedInvoiceData data, string? blobUrl = null, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var invoice = new Invoice
        {
            TenantId = tenantId,
            VendorName = data.VendorName,
            VendorGstin = data.VendorGstin,
            InvoiceNumber = data.InvoiceNumber,
            InvoiceDate = data.InvoiceDate ?? DateTime.UtcNow.Date,
            TotalAmount = data.TotalAmount,
            TaxableAmount = data.TaxableAmount,
            Cgst = data.Cgst,
            Sgst = data.Sgst,
            Igst = data.Igst,
            Category = string.IsNullOrWhiteSpace(data.Category) ? "Uncategorized" : data.Category,
            BlobUrl = blobUrl,
            OriginalFileName = fileName,
            Status = InvoiceStatus.Parsed
        };

        foreach (var item in data.LineItems)
        {
            invoice._lineItems.Add(InvoiceLineItem.Create(
                invoice.Id, item.Description, item.Quantity, item.UnitPrice, item.Amount, item.GstRate));
        }

        invoice.RaiseEvent(new InvoiceParsedEvent(
            invoice.Id, tenantId, invoice.VendorName, invoice.TotalAmount));

        return invoice;
    }

    public void FlagAnomaly(string reason, decimal expectedAmount)
    {
        IsAnomaly = true;
        AnomalyReason = reason;
        Status = InvoiceStatus.AnomalyFlagged;
        RaiseEvent(new AnomalyDetectedEvent(Id, TenantId, reason, TotalAmount, expectedAmount));
        Touch();
    }

    public void Verify()
    {
        Status = InvoiceStatus.Verified;
        Touch();
    }

    public string ToSummary() =>
        $"{InvoiceDate:yyyy-MM-dd} | {VendorName} | {Category} | ₹{TotalAmount:N2} | GSTIN:{VendorGstin ?? "N/A"}";
}
