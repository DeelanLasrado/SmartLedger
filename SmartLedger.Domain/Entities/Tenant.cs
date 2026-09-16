using SmartLedger.Domain.Common;
using SmartLedger.Domain.Enums;

namespace SmartLedger.Domain.Entities;

public class Tenant : BaseEntity
{
    public string BusinessName { get; private set; } = string.Empty;
    public string? Gstin { get; private set; }
    public string OwnerEmail { get; private set; } = string.Empty;
    public SubscriptionTier Tier { get; private set; } = SubscriptionTier.Free;
    public int MonthlyInvoiceLimit { get; private set; } = 50;
    public int InvoicesUsedThisMonth { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Tenant() { }

    public static Tenant Create(string businessName, string ownerEmail, string? gstin = null)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new ArgumentException("Business name is required.", nameof(businessName));
        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new ArgumentException("Owner email is required.", nameof(ownerEmail));

        return new Tenant
        {
            BusinessName = businessName.Trim(),
            OwnerEmail = ownerEmail.Trim().ToLowerInvariant(),
            Gstin = gstin?.Trim().ToUpperInvariant(),
            Tier = SubscriptionTier.Free,
            MonthlyInvoiceLimit = 50
        };
    }

    public void UpgradeToPro()
    {
        Tier = SubscriptionTier.Pro;
        MonthlyInvoiceLimit = int.MaxValue;
        Touch();
    }

    public bool CanUploadInvoice() =>
        IsActive && (Tier == SubscriptionTier.Pro || InvoicesUsedThisMonth < MonthlyInvoiceLimit);

    public void IncrementInvoiceUsage()
    {
        InvoicesUsedThisMonth++;
        Touch();
    }

    public void ResetMonthlyUsage()
    {
        InvoicesUsedThisMonth = 0;
        Touch();
    }
}
