using SmartLedger.Domain.Common;

namespace SmartLedger.Domain.Entities;

public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? GstRate { get; private set; }

    private InvoiceLineItem() { }

    public static InvoiceLineItem Create(
        Guid invoiceId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal amount,
        decimal? gstRate)
    {
        return new InvoiceLineItem
        {
            InvoiceId = invoiceId,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = amount,
            GstRate = gstRate
        };
    }
}
