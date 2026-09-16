using FluentValidation;
using MediatR;
using SmartLedger.Application.Invoices.DTOs;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Application.Invoices.Commands;

public record ParseInvoiceCommand(Guid TenantId, Stream InvoiceFile, string FileName)
    : IRequest<InvoiceParseResult>;

public class ParseInvoiceCommandValidator : AbstractValidator<ParseInvoiceCommand>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".tiff", ".bmp"];

    public ParseInvoiceCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.InvoiceFile).NotNull();
        RuleFor(x => x.FileName)
            .Must(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .WithMessage("Unsupported file type. Use PDF or image.");
    }
}

public class ParseInvoiceHandler(
    IDocumentIntelligenceService docService,
    IInvoiceRepository invoiceRepo,
    ITenantRepository tenantRepo,
    IAnomalyDetector anomalyDetector,
    IUnitOfWork unitOfWork,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore) : IRequestHandler<ParseInvoiceCommand, InvoiceParseResult>
{
    public async Task<InvoiceParseResult> Handle(ParseInvoiceCommand cmd, CancellationToken ct)
    {
        var tenant = await tenantRepo.GetByIdAsync(cmd.TenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        if (!tenant.CanUploadInvoice())
            throw new InvalidOperationException(
                $"Monthly invoice limit reached ({tenant.MonthlyInvoiceLimit}). Upgrade to Pro for unlimited uploads.");

        var blobUrl = await docService.UploadAsync(cmd.InvoiceFile, cmd.FileName, ct);
        cmd.InvoiceFile.Position = 0;
        var extracted = await docService.ExtractInvoiceFieldsAsync(cmd.InvoiceFile, cmd.FileName, ct);

        var invoice = Invoice.Create(cmd.TenantId, extracted, blobUrl, cmd.FileName);
        var anomaly = await anomalyDetector.CheckAsync(invoice, ct);
        if (anomaly.IsAnomaly)
            invoice.FlagAnomaly(anomaly.Reason, anomaly.ExpectedAmount);

        await invoiceRepo.AddAsync(invoice, ct);
        tenant.IncrementInvoiceUsage();
        await tenantRepo.UpdateAsync(tenant, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var summary = invoice.ToSummary();
        var embedding = await embeddingService.EmbedAsync(summary, ct);
        await vectorStore.UpsertAsync(cmd.TenantId, invoice.Id, summary, embedding, ct);

        return new InvoiceParseResult(invoice.Id, extracted, anomaly);
    }
}
