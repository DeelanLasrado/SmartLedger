using SmartLedger.Domain.Enums;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Application.Invoices.DTOs;

public record InvoiceDto(
    Guid Id,
    Guid TenantId,
    string VendorName,
    string? VendorGstin,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    decimal TotalAmount,
    decimal TaxableAmount,
    decimal Cgst,
    decimal Sgst,
    decimal Igst,
    string Category,
    InvoiceStatus Status,
    bool IsAnomaly,
    string? AnomalyReason,
    IReadOnlyList<InvoiceLineItemDto> LineItems);

public record InvoiceLineItemDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount,
    decimal? GstRate);

public record InvoiceParseResult(
    Guid InvoiceId,
    ExtractedInvoiceData Extracted,
    AnomalyResult Anomaly);
