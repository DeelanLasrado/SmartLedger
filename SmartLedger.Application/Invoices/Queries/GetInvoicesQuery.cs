using MediatR;
using SmartLedger.Application.Invoices.DTOs;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Application.Invoices.Queries;

public record GetInvoicesQuery(Guid TenantId, int Page = 1, int PageSize = 20)
    : IRequest<IReadOnlyList<InvoiceDto>>;

public class GetInvoicesHandler(IInvoiceRepository invoiceRepo)
    : IRequestHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
        var invoices = await invoiceRepo.GetByTenantAsync(request.TenantId, skip, request.PageSize, ct);

        return invoices.Select(i => new InvoiceDto(
            i.Id, i.TenantId, i.VendorName, i.VendorGstin, i.InvoiceNumber, i.InvoiceDate,
            i.TotalAmount, i.TaxableAmount, i.Cgst, i.Sgst, i.Igst, i.Category, i.Status,
            i.IsAnomaly, i.AnomalyReason,
            i.LineItems.Select(l => new InvoiceLineItemDto(
                l.Description, l.Quantity, l.UnitPrice, l.Amount, l.GstRate)).ToList()
        )).ToList();
    }
}
