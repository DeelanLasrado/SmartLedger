using FluentAssertions;
using Moq;
using SmartLedger.Application.Invoices.Commands;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Tests.Application;

public class ParseInvoiceHandlerTests
{
    [Fact]
    public async Task Handle_Parses_Stores_And_Embeds_Invoice()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Tenant.Create("Demo", "demo@test.com");
        // Force known id via reflection-free approach: use the created tenant id
        tenantId = tenant.Id;

        var extracted = new ExtractedInvoiceData
        {
            VendorName = "Vendor",
            TotalAmount = 1000m,
            TaxableAmount = 847.46m,
            Cgst = 76.27m,
            Sgst = 76.27m,
            Category = "Inventory",
            LineItems = [new ExtractedLineItem("Goods", 1, 847.46m, 847.46m, 18)]
        };

        var doc = new Mock<IDocumentIntelligenceService>();
        doc.Setup(d => d.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/test.pdf");
        doc.Setup(d => d.ExtractInvoiceFieldsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(extracted);

        var invoiceRepo = new Mock<IInvoiceRepository>();
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var anomaly = new Mock<IAnomalyDetector>();
        anomaly.Setup(a => a.CheckAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnomalyResult(false, "ok", 1000m));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var embedding = new Mock<IEmbeddingService>();
        embedding.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });

        var vectors = new Mock<IVectorStore>();

        var handler = new ParseInvoiceHandler(
            doc.Object, invoiceRepo.Object, tenantRepo.Object,
            anomaly.Object, uow.Object, embedding.Object, vectors.Object);

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await handler.Handle(
            new ParseInvoiceCommand(tenantId, stream, "invoice.pdf"), CancellationToken.None);

        result.InvoiceId.Should().NotBeEmpty();
        result.Extracted.VendorName.Should().Be("Vendor");
        invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        vectors.Verify(v => v.UpsertAsync(tenantId, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_When_Monthly_Limit_Reached()
    {
        var tenant = Tenant.Create("Demo", "demo@test.com");
        for (var i = 0; i < 50; i++)
            tenant.IncrementInvoiceUsage();

        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(t => t.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var handler = new ParseInvoiceHandler(
            Mock.Of<IDocumentIntelligenceService>(),
            Mock.Of<IInvoiceRepository>(),
            tenantRepo.Object,
            Mock.Of<IAnomalyDetector>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IEmbeddingService>(),
            Mock.Of<IVectorStore>());

        await using var stream = new MemoryStream([1]);
        var act = async () => await handler.Handle(
            new ParseInvoiceCommand(tenant.Id, stream, "a.pdf"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Monthly invoice limit*");
    }
}
