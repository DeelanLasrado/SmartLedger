using MediatR;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Enums;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Application.Gst.Commands;

public record GstEntryInput(
    GstReturnType ReturnType,
    string? CounterpartyGstin,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    decimal TaxableValue,
    decimal Igst,
    decimal Cgst,
    decimal Sgst);

public record ImportGstEntriesCommand(Guid TenantId, string Period, IReadOnlyList<GstEntryInput> Entries)
    : IRequest<int>;

public class ImportGstEntriesHandler(IGstEntryRepository gstRepo, IUnitOfWork unitOfWork)
    : IRequestHandler<ImportGstEntriesCommand, int>
{
    public async Task<int> Handle(ImportGstEntriesCommand request, CancellationToken ct)
    {
        var entities = request.Entries.Select(e => GstEntry.Create(
            request.TenantId, e.ReturnType, request.Period, e.CounterpartyGstin,
            e.InvoiceNumber, e.InvoiceDate, e.TaxableValue, e.Igst, e.Cgst, e.Sgst)).ToList();

        await gstRepo.AddRangeAsync(entities, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return entities.Count;
    }
}
