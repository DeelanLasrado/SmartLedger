using FluentAssertions;
using Moq;
using SmartLedger.AI.AnomalyDetection;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartLedger.Tests.AI;

public class AnomalyDetectorTests
{
    [Fact]
    public async Task CheckAsync_Flags_When_Amount_Exceeds_3x_Average()
    {
        var tenantId = Guid.NewGuid();
        var history = new List<Invoice>
        {
            Invoice.Create(tenantId, new ExtractedInvoiceData { VendorName = "A", TotalAmount = 1000, Category = "Fuel" }),
            Invoice.Create(tenantId, new ExtractedInvoiceData { VendorName = "B", TotalAmount = 1200, Category = "Fuel" }),
            Invoice.Create(tenantId, new ExtractedInvoiceData { VendorName = "C", TotalAmount = 1100, Category = "Fuel" })
        };

        var candidate = Invoice.Create(tenantId, new ExtractedInvoiceData
        {
            VendorName = "Spike",
            TotalAmount = 10000,
            Category = "Fuel"
        });

        var repo = new Mock<IInvoiceRepository>();
        repo.Setup(r => r.GetRecentByCategoryAsync(tenantId, "Fuel", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var detector = new AnomalyDetector(repo.Object, NullLogger<AnomalyDetector>.Instance);
        var result = await detector.CheckAsync(candidate);

        result.IsAnomaly.Should().BeTrue();
        result.Reason.Should().Contain("unusual");
        result.ExpectedAmount.Should().BeApproximately(1100m, 50m);
    }

    [Fact]
    public async Task CheckAsync_Skips_When_Insufficient_History()
    {
        var tenantId = Guid.NewGuid();
        var candidate = Invoice.Create(tenantId, new ExtractedInvoiceData
        {
            VendorName = "Only",
            TotalAmount = 50000,
            Category = "Telecom"
        });

        var repo = new Mock<IInvoiceRepository>();
        repo.Setup(r => r.GetRecentByCategoryAsync(tenantId, "Telecom", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        var detector = new AnomalyDetector(repo.Object, NullLogger<AnomalyDetector>.Instance);
        var result = await detector.CheckAsync(candidate);

        result.IsAnomaly.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient");
    }
}
