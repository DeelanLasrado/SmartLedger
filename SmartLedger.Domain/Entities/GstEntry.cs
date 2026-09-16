using SmartLedger.Domain.Common;
using SmartLedger.Domain.Enums;

namespace SmartLedger.Domain.Entities;

public class GstEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public GstReturnType ReturnType { get; private set; }
    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public string? CounterpartyGstin { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public DateTime? InvoiceDate { get; private set; }
    public decimal TaxableValue { get; private set; }
    public decimal Igst { get; private set; }
    public decimal Cgst { get; private set; }
    public decimal Sgst { get; private set; }
    public decimal TotalTax => Igst + Cgst + Sgst;

    private GstEntry() { }

    public static GstEntry Create(
        Guid tenantId,
        GstReturnType returnType,
        string period,
        string? counterpartyGstin,
        string? invoiceNumber,
        DateTime? invoiceDate,
        decimal taxableValue,
        decimal igst,
        decimal cgst,
        decimal sgst)
    {
        return new GstEntry
        {
            TenantId = tenantId,
            ReturnType = returnType,
            Period = period,
            CounterpartyGstin = counterpartyGstin,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            TaxableValue = taxableValue,
            Igst = igst,
            Cgst = cgst,
            Sgst = sgst
        };
    }

    public string MatchKey =>
        $"{CounterpartyGstin}|{InvoiceNumber}|{TaxableValue:F2}".ToUpperInvariant();
}
