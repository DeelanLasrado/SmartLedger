using FluentAssertions;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Enums;

namespace SmartLedger.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void Free_Tier_Enforces_Monthly_Limit()
    {
        var tenant = Tenant.Create("Kirana", "owner@test.com");
        tenant.Tier.Should().Be(SubscriptionTier.Free);
        tenant.MonthlyInvoiceLimit.Should().Be(50);

        for (var i = 0; i < 50; i++)
            tenant.IncrementInvoiceUsage();

        tenant.CanUploadInvoice().Should().BeFalse();
    }

    [Fact]
    public void UpgradeToPro_Removes_Limit()
    {
        var tenant = Tenant.Create("Kirana", "owner@test.com");
        for (var i = 0; i < 50; i++)
            tenant.IncrementInvoiceUsage();

        tenant.UpgradeToPro();
        tenant.Tier.Should().Be(SubscriptionTier.Pro);
        tenant.CanUploadInvoice().Should().BeTrue();
    }

    [Fact]
    public void Create_Requires_Business_And_Email()
    {
        var act = () => Tenant.Create("", "a@b.com");
        act.Should().Throw<ArgumentException>();
    }
}
