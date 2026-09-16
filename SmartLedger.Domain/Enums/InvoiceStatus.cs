namespace SmartLedger.Domain.Enums;

public enum InvoiceStatus
{
    Pending = 0,
    Parsed = 1,
    Verified = 2,
    AnomalyFlagged = 3,
    Rejected = 4
}
