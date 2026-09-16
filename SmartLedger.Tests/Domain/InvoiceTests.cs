using FluentAssertions;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Enums;
using SmartLedger.Domain.ValueObjects;

namespace SmartLedger.Tests.Domain;

public class InvoiceTests
{
    [Fact]
    public void Create_Maps_Extracted_Data_And_Raises_Event()
    {
        var tenantId = Guid.NewGuid();
        var data = new ExtractedInvoiceData
        {
            VendorName = "Test Vendor",
            VendorGstin = "27AABCR1234A1Z5",
            InvoiceNumber = "INV-1",
            InvoiceDate = new DateTime(2026, 1, 15),
            TotalAmount = 1180m,
            TaxableAmount = 1000m,
            Cgst = 90m,
            Sgst = 90m,
            Category = "Inventory",
            LineItems = [new ExtractedLineItem("Item A", 2, 500, 1000, 18)]
        };

        var invoice = Invoice.Create(tenantId, data, "/uploads/a.pdf", "a.pdf");

        invoice.TenantId.Should().Be(tenantId);
        invoice.VendorName.Should().Be("Test Vendor");
        invoice.TotalAmount.Should().Be(1180m);
        invoice.Status.Should().Be(InvoiceStatus.Parsed);
        invoice.LineItems.Should().HaveCount(1);
        invoice.DomainEvents.Should().NotBeEmpty();
        invoice.ToSummary().Should().Contain("Test Vendor");
    }

    [Fact]
    public void FlagAnomaly_Sets_Flags_And_Status()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), new ExtractedInvoiceData
        {
            VendorName = "V",
            TotalAmount = 50000m,
            Category = "Fuel"
        });

        invoice.FlagAnomaly("Too high", 10000m);

        invoice.IsAnomaly.Should().BeTrue();
        invoice.AnomalyReason.Should().Be("Too high");
        invoice.Status.Should().Be(InvoiceStatus.AnomalyFlagged);
        invoice.DomainEvents.Should().Contain(e => e.GetType().Name.Contains("Anomaly"));
    }
}
